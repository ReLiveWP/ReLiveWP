---
id: 400102
type: PRB
title: Sign in appears to succeed, but you then see an "invalid username or password" message when syncing with Windows Live.
summary: "You see an 'invalid username or password' message when syncing with Windows Live, and cannot access account settings."
applies_to:
  - Windows Phone 7.0
  - Windows Phone 7.5 (Mango)
  - Windows Phone 7.8
  - ReLiveWP Exchange ActiveSync service
error_codes: []
keywords: [sign-in, login, live id, exchange, mail, contacts, calendar]
fwlink: 2102
revision: "1.0"
last_review: 2026-08-07
see_also: [400101]
---

## Symptoms

You are able to sign in to Windows Live on the phone, but when you try to sync, you see an "Attention required" or "Not up-to-date" label on the account, and tapping it shows the following message:

```
Windows Live password is incorrect
```

## Cause

Your device may be trying to connect to Microsoft's ActiveSync server instead of ReLiveWP's. Usually, this manifests as a connection timeout or failure because Microsoft's official servers require modern TLS support, however if you are using certain types of proxy, specifically those that will decrypt TLS traffic, you may see the "invalid username or password" message instead. 

## Resolution

Attempt the following steps:

1. **Disable any proxy.** If you are using a proxy, disable it and try syncing again. If syncing now fails, and you do not see the "invalid username or password" message, move on to step 5.

2. **Connect to Wi-Fi.** If you are on a cellular connection, try connecting to Wi-Fi and syncing again. If syncing now fails, move on to step 5.

3. **Try a different network.** If you are on Wi-Fi, try connecting to a different Wi-Fi network and syncing again. If syncing now fails, move on to step 5.

4. **Disconnect from all networks.** If you are on Wi-Fi, disconnect from it, and if you are on cellular, turn on Airplane mode. Then try syncing again. The device may take some time to realize it is offline, so wait a few minutes.

5. **Change the server setting.** In **Settings**, then **email+accounts**, tap the Windows Live account, dismiss the error, then scroll down to **Server**. If it is set to `m.hotmail.com`, change it to `sync.relivewp.net`. Then tap the checkmark and try syncing again. Syncing should now succeed.

## Status

We are currently investigating a patch to update the default server settings. In the meantime, you can fix the problem by changing the server as described above.