---
id: 400101
type: PRB
title: You cannot sync with Windows Live because of a connection error.
summary: "The message 'We're having a problem connecting to m.hotmail.com' is shown when attempting to sync with Windows Live."
applies_to:
  - Windows Phone 7.0
  - Windows Phone 7.5 (Mango)
  - Windows Phone 7.8
  - ReLiveWP Exchange ActiveSync service
error_codes: ["0x85010016", "0x80072EFF", "0x8007EFD"]
keywords: [sign-in, login, live id, exchange, mail, contacts, calendar]
fwlink: 2101
revision: "1.0"
last_review: 2026-08-07
see_also: []
---

## Symptoms

In email+accounts, Windows Live has an "Attention required" or "Not up-to-date" label. When you tap it, you see the following message:

```
Not updated

We're having a problem connecting to m.hotmail.com. Try again later.
```

With the error code `0x85010016`, `0x80072EFF` or `0x8007EFD`.

## Cause

Your device may be trying to connect to Microsoft's ActiveSync server instead of ReLiveWP's. Microsoft's sever now requires modern TLS, so Windows Phone will report connection reset and TLS errors instead of the usual "invalid username or password" message.

## Resolution

In **Settings**, then **email+accounts**, tap the Windows Live account, dismiss the error, then scroll down to **Server**. If it is set to `m.hotmail.com`, change it to `sync.relivewp.net`. Then tap the checkmark and try syncing again. Syncing should now succeed.

## Status

We are currently investigating a patch to update the default server settings. In the meantime, you can fix the problem by changing the server as described above.