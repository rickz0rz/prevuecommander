using System.Threading.Channels;
using Prevue.Commands;

namespace PrevueCommander.Server;

/// <summary>The state of a job of the web server.</summary>
public enum JobState
{
    Queued,
    Sending,
    Done,
    Failed,
    Cancelled
}

/// <summary>The information about a job, for the HTTP API.</summary>
/// <param name="Seconds">The expected time to send the job.</param>
public sealed record JobInfo(
    int Id,
    string Name,
    string Queue,
    JobState State,
    int Commands,
    long Bytes,
    long BytesSent,
    double Seconds,
    DateTime Created,
    DateTime? Started,
    DateTime? Finished,
    string? Error);

/// <summary>A group of commands that the web server sends together.</summary>
public sealed class Job
{
    private long _bytesSent;
    private long _controlBytesLeft;

    internal Job(int id, string name, IReadOnlyList<BaseCommand> commands, double seconds, DateTime created)
    {
        Id = id;
        Name = name;
        Commands = commands;
        Seconds = seconds;
        Created = created;
        Bytes = commands.Sum(c => (long)c.Render().Length);
        _controlBytesLeft = commands.Where(c => c.Line == SerialLine.Control).Sum(c => (long)c.Render().Length);
        UsesDataLine = commands.Any(c => c.Line == SerialLine.Data);
    }

    public int Id { get; }
    public string Name { get; }
    public IReadOnlyList<BaseCommand> Commands { get; }
    public long Bytes { get; }
    public double Seconds { get; }
    public DateTime Created { get; }
    public DateTime? Started { get; internal set; }
    public DateTime? Finished { get; internal set; }
    public JobState State { get; internal set; } = JobState.Queued;
    public string? Error { get; internal set; }

    /// <summary>True if the job has a data line command. Such a job goes to the data queue.</summary>
    public bool UsesDataLine { get; }

    /// <summary>The control line bytes that the job did not send yet.</summary>
    public long ControlBytesLeft => Interlocked.Read(ref _controlBytesLeft);

    internal void Sent(BaseCommand command)
    {
        var bytes = command.Render().Length;
        Interlocked.Add(ref _bytesSent, bytes);
        if (command.Line == SerialLine.Control)
            Interlocked.Add(ref _controlBytesLeft, -bytes);
    }

    public JobInfo Info() => new(Id, Name, UsesDataLine ? "data" : "control", State, Commands.Count, Bytes,
        Interlocked.Read(ref _bytesSent), Math.Round(Seconds, 1), Created, Started, Finished, Error);
}

/// <summary>
/// Sends the jobs of the web server in order. The control queue and the data queue each have one worker, so a promo
/// does not wait for a long file transfer on the data line.
/// </summary>
/// <remarks>
/// A job that has started always finishes, because ESQ cannot use a record that stops in the middle. Only a queued job
/// can be cancelled. The queue keeps the last <see cref="KeptJobs"/> jobs for the status.
/// </remarks>
/// <param name="sink">Sends the commands of a job together.</param>
/// <param name="time">The expected time of one command.</param>
/// <param name="clock">The local time.</param>
public sealed class JobQueue(ICommandSink sink, Func<BaseCommand, TimeSpan> time, Func<DateTime> clock, TextWriter log)
{
    public const int KeptJobs = 50;

    private readonly Channel<Job> _control = Channel.CreateUnbounded<Job>();
    private readonly Channel<Job> _data = Channel.CreateUnbounded<Job>();
    private readonly List<Job> _jobs = [];
    private int _lastId;

    /// <summary>Adds a job at the end of its queue.</summary>
    public Job Enqueue(string name, IReadOnlyList<BaseCommand> commands)
    {
        if (commands.Count == 0)
            throw new ArgumentException("The request has no commands to send.");

        var seconds = commands.Sum(c => time(c).TotalSeconds);
        var job = new Job(Interlocked.Increment(ref _lastId), name, commands, seconds, clock());
        lock (_jobs)
        {
            _jobs.Add(job);
            if (_jobs.Count > KeptJobs)
            {
                // Remove the oldest finished job. A queued or a sending job stays.
                var old = _jobs.FirstOrDefault(j => j.State is not (JobState.Queued or JobState.Sending));
                if (old != null)
                    _jobs.Remove(old);
            }
        }

        (job.UsesDataLine ? _data : _control).Writer.TryWrite(job);
        return job;
    }

    /// <summary>Cancels a queued job.</summary>
    /// <returns>False if the job does not exist or has started.</returns>
    public bool Cancel(int id)
    {
        lock (_jobs)
        {
            var job = _jobs.FirstOrDefault(j => j.Id == id);
            if (job is not { State: JobState.Queued })
                return false;
            job.State = JobState.Cancelled;
            job.Finished = clock();
            return true;
        }
    }

    public JobInfo? Find(int id)
    {
        lock (_jobs)
            return _jobs.FirstOrDefault(j => j.Id == id)?.Info();
    }

    /// <summary>The jobs, the newest first.</summary>
    public List<JobInfo> Jobs()
    {
        lock (_jobs)
            return _jobs.Select(j => j.Info()).Reverse().ToList();
    }

    /// <summary>The jobs that are queued or sending, in each queue.</summary>
    public (int Control, int Data) Waiting()
    {
        lock (_jobs)
        {
            var open = _jobs.Where(j => j.State is JobState.Queued or JobState.Sending).ToList();
            return (open.Count(j => !j.UsesDataLine), open.Count(j => j.UsesDataLine));
        }
    }

    /// <summary>The control line bytes of the jobs that the server did not send yet.</summary>
    public long ControlBytesWaiting()
    {
        lock (_jobs)
            return _jobs.Where(j => j.State is JobState.Queued or JobState.Sending).Sum(j => j.ControlBytesLeft);
    }

    /// <summary>Sends the jobs of the two queues until the cancellation.</summary>
    public Task RunAsync(CancellationToken cancellationToken) =>
        Task.WhenAll(WorkAsync(_control.Reader, cancellationToken), WorkAsync(_data.Reader, cancellationToken));

    private async Task WorkAsync(ChannelReader<Job> queue, CancellationToken cancellationToken)
    {
        await foreach (var job in queue.ReadAllAsync(cancellationToken))
        {
            lock (_jobs)
            {
                if (job.State != JobState.Queued)
                    continue;
                job.State = JobState.Sending;
                job.Started = clock();
            }

            try
            {
                await sink.SendBatchAsync(job.Commands, job.Sent, cancellationToken);
                job.State = JobState.Done;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                job.State = JobState.Cancelled;
                throw;
            }
            catch (Exception e) when (e is IOException or System.Net.Sockets.SocketException
                                          or InvalidOperationException or UnauthorizedAccessException)
            {
                // A connection error fails this job only. The next job tries to connect again.
                job.State = JobState.Failed;
                job.Error = e.Message;
                log.WriteLine($"Job {job.Id} ({job.Name}) failed: {e.Message}");
            }
            finally
            {
                job.Finished = clock();
            }
        }
    }
}
