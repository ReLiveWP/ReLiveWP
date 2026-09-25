---
id: 100101
type: HOWTO
title: Installing ReLiveWP on Lumia devices (Linux/macOS)
summary: How to install ReLiveWP on supported Lumia devices.
applies_to:
  - Nokia Lumia 800
  - Nokia Lumia 710
keywords: [flash, getting started, lumia]
fwlink: 1101
revision: "1.0"
last_review: 2026-08-07
see_also: []
---

# WARNINGS: PLEASE READ CAREFULLY
- Installing ReLiveWP requires flashing a custom ROM. This process is inherently risky and while great care has been taken to make these instructions as thorough and accurate as possible, we take NO RESPONSIBILITY for ANY DAMAGE to your device caused during this process. 
- **All data on the device will be lost and will not be recoverable**, Zune backups will not be restorable.
- No ReLiveWP ROMs are carrier branded. If you are using a carrier branded device, you will lose all carrier specific features, apps and branding. **This will not unlock a carrier locked device.**
- This process will not work on Lumia devices that do not have a Qualcomm bootloader. Please check the [device support page](/kb/100002) to see if your device is supported.
- This is mostly a generic flashing guide and can be used to flash any custom ROM. Other flashing methods do exist, but this is the one we recommend. 

<!-- 
# Conventions used in this guide
- Basic familiarity with the command line is assumed. Where terminal commands are provided:
  - `$` indicates a command to be run in a non-root shell, and should be run as the current user.
  - `#` indicates a command to be run in a root shell, and should be run as the root user (i.e. using `sudo`).
  - `<placeholder>` indicates a section that you must replace with your own value, without the angle brackets.  
-->

# Getting started
Before you begin, you will need the following:
- A Linux or macOS machine (Intel or Apple Silicon)
- At least 8GB of free disk space to backup a Lumia 710, at least 16GB for the Lumia 800
  - You may be tempted to avoid taking a full backup of your device, **we strongly recommend that you do.** These backups often contain invaluable information and cached data that we can use to improve ReLiveWP, and can also be used to restore your device to its original state if something goes wrong.
- A working micro-USB cable that can transfer data.
- A free USB port that does not go through a hub.
- ~30 minutes of uninterrupted time and a power source.

And of course
- A Lumia 710 or Lumia 800 charged to at least 50%
  - These devices are old and their batteries are tired, please make sure your device can run off the charger for at least an hour before proceeding.
- An appropriate ReLiveWP ROM for your device. 
- The appropriate Qualcomm bootloader for your device.
- A copy of `lumia-dloadtool`.

You can find these [through the ReLiveWP Download Center](https://downloads.relivewp.net).

## Preparing your system
Firstly, Linux users with a graphical desktop environment should disable automatic disk mounting. This is to prevent the system from trying to mount the device while it is in flash mode which can cause issues with the flashing process and **potentially brick your device if a system service tries to write to it while in flash mode**.

Explicit instructions depend on your system and distro, they will not be provided here, but you probably want to neuter `udisks2` :)

While macOS does not provide a way to disable automatic disk mounting, it is generally safe and has not been known to cause issues. If you are concerned, you should use the `diskutil` command to unmount the device before flashing, or use a Linux machine instead.

## Installing `lumia-dloadtool`
TBD.

## Entering flash mode
With the device fully shut down, hold the **Vol+** button and then hold **Power** (or plug the device into your computer) until you feel a short vibration. There will be nothing on screen. While in Nokia DLOAD, your device will reboot after ~45 seconds, so you may need to do this a few times, commit whichever process works to memory!

Some devices are very picky about entering their bootloader, keep trying! On the Lumia 710 you may find it more reliable to remove the battery and hold Vol+ while plugging the device into your system.

Once in bootloader mode, run `lumia-dloadtool`. If you see

```
# lumia-dloadtool
a device in Qualcomm mode was found, this is only compatible with DLOAD bootloaders
```

then you're already set! The Qualcomm loader has already been flashed. If you got this phone "new-in-box" from China you can start requesting a refund, you can also skip to [Taking a Backup](#taking-a-backup)! But if you see

```
# lumia-dloadtool
successfully detected DLOAD device!
DLOAD build timestamp: ....
```

then we need to flash the Qualcomm loader to your device.

If you see either `a device in the WP7 bootloader was found, please put it into DLOAD mode` or `no devices detected` you'll need to try again, check your cable, make sure your volume buttons actually work (some get a little sticky over time), etc.