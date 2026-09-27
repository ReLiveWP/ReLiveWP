---
id: 100001
type: INFO
title: Introduction to ReLiveWP.
summary: An overview of the ReLiveWP project and the Windows Live services we currently support.
applies_to:
  - Windows Phone 7.0
  - Windows Phone 7.5 (Mango)
  - Windows Phone 7.8
keywords: [overview, getting started, services, windows live]
fwlink: 1001
revision: "1.0"
last_review: 2026-08-07
see_also: [100002]
---

## Summary
When Windows Phone 7 launched in 2010, it heavily relied on Microsoft's Windows Live services for core functionality. Everything from the Marketplace to Push&nbsp;Notifications to the People Hub to Find my Phone all depend on a functional Windows Live login. In the years since however, Microsoft has slowly discontinued these services, seriously limiting what these devices can do. With no sync, no Marketplace, and no way to sign in to Windows Live, these smartphones have turned pretty dumb. 

ReLiveWP changes that. It is an open-source reimplementation of the Windows Live services used by Windows Phone 7, with the explicit goal of making these devices sing again. Instead of talking to Microsoft servers, your phone talks to ReLiveWP, and the parts of the operating system that depended on Windows Live light up [like it's 2011 all over again](https://www.youtube.com/watch?v=KQ6zr6kCPj8).

## More information

### Supported services
ReLiveWP is currently a work in progress. The following services are currently implemented and supported:

| Service | Description |
| --- | --- |
| **Windows Live ID Login** | Allows you to sign in with your ReLiveWP account on a Windows Phone device. |
| **Device Activation** | Issues device activation certificates to Windows Phone devices, required to enable sign-in. |
| **Mail, Calendar and People** | Replaces Microsoft's Exchange ActiveSync server, allowing you to sync mail, calendar and contacts with your ReLiveWP account. |
| **Windows Live People** | Shows social feeds and contacts in the People hub, and allows you to post status updates. |
| **SkyDrive Photos** | Allows the Pictures hub to backup and sync your photos. |
| **SkyDrive Files** | Allows the Office hub to open and save files from SkyDrive. |
| **Find My Phone** | Allows you to locate your phone, lock it, or wipe it remotely. |
| **Push Notifications** | Allows apps to receive push notifications. |

### Design philosophy
ReLiveWP is a bridge between your old phone and the modern web, instead of storing your data ourselves, we try to connect your phone to the services you already use, and not just Microsoft's! For example, instead of storing your photos on our servers, we connect the Pictures hub to your OneDrive or Google Photos account. Here are the connected services we currently support:

| Service | Supported services |
| --- | --- |
| **Social** | Bluesky |
| **SkyDrive Photos** | Microsoft OneDrive, Google Photos |
| **SkyDrive Files** | Microsoft OneDrive |

with many more to follow! Check back here for updates.

### Supported devices:
Please see the [What devices support ReLiveWP?](/kb/100002) article for a list of supported devices.

## References

The project source, including the list of original hostnames each service stands in for, is at
<https://github.com/wamwoowam/ReLiveWP>.
