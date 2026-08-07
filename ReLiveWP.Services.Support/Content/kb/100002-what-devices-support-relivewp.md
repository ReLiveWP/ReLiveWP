---
id: 100002
type: INFO
title: What devices support ReLiveWP?
summary: Currently, the Lumia 800 and Lumia 710 are the best supported devices.
applies_to:
  - Windows Phone 7.0
  - Windows Phone 7.5 (Mango)
  - Windows Phone 7.8
keywords: [overview, getting started, services, windows live]
fwlink: 1002
revision: "1.0"
last_review: 2026-08-07
see_also: []
---

## Summary
ReLiveWP aims to support as many Windows Phone 7 devices as possible, but that list is quite limited because there is no universal exploit for Windows Phone 7 devices. Each device must have custom, device specific ROMs built for it, and those ROMs must be tested to ensure they work correctly. 

## More information

The current state of device support is as follows:

| Device | Status | Notes |
| --- | --- | --- |
| **Nokia Lumia 800** | ✓ | Requires Qualcomm bootloader. This is the best supported device, and the most common test device we use. |
| **Nokia Lumia 710** | ✓ | Requires Qualcomm bootloader. |
| **Nokia Lumia 900** | ✗ | No known bootloader exploit. |
| **Nokia Lumia 610** | ✗ | No known bootloader exploit. |
| **HTC HD2** | ⚠ | Verified to work, usual caveats apply when running Windows Phone 7 on an unofficial device. |
| **HTC HD7** | ? | Untested, assumed to work given the HD2 does. |
| **Samsung Omnia 7** | ? | I have one, not cooked a ROM yet. - Wam |

No ROMs are currently available publicly, but if you are a developer and want to help build and test ROMs for your device, please contact us. 

In future, the ReLiveWP Adaption Kit will allow ROM developers to integrate ReLiveWP support into their own custom ROMs, stay tuned for more information.


