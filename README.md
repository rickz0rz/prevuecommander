# Prevue Commander

## What is this?
Prevue Commander is an application (very WIP still) that can be used to send configuration data to an emulated Prevue Guide system.

## What is Prevue Guide?
If you grew up in the 80s/90s and had cable TV, you may remember [this channel](https://www.youtube.com/watch?v=ceukujHQvwM) somewhere in your line-up. As it turns out, this software ran on an Amiga somewhere at your cable company, and the software that ran on these machines [was recovered](https://prevueguide.com/wiki/Prevue_(ESQ)).

## So what can this do?
Currently, this software can send channel line-ups and listings data generated in XMLTV format. Listings data for current TV programs are easily obtainable from zap2it through the use of [zap2xml](https://github.com/jef/zap2xml). In addition to this, Prevue Commander can send basic text advertisements that will cycle on the screen while the listings are being displayed.

## How do I run this?
First off, you'll need to setup a working Prevue Guide build-out. You can follow instructions [here](https://prevueguide.neocities.org/guides/Esquire.html) to get started on this. Once you've done this, you'll need to check what port the 2400 baud data connection is set to in your Amiga emulator of choice. Set the proper port in the code (this is hardcoded right now as well as the 'playbook' of commands to run), supply a valid XMLTV xml file, and you should see listings data populate after running.

## What's next on the menu?
There's a few things I'd like to add to this to give it more flexibility. Those include:
- Pulling guide data from a Channels DVR (as that's what I currently use)
- Not having the "playbook" of commands hardcoded
- Incremental updates (sending new listings data without sending the channel line-up)
- Better support/documentation around some of the lesser understood control commands
- Support for binary file sending to the Prevue installation (useful for sending graphical ads)
- Automated conversion of images to graphical ads
- I'm sure there's more...
