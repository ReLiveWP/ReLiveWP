---
id: 300142
type: PRB
title: You cannot sign in to Windows Live on a Windows Phone device.
summary: Sign-in fails and the account is never added.
applies_to:
  - Windows Phone 7.0
  - Windows Phone 7.5 (Mango)
  - Windows Phone 7.8
  - ReLiveWP Windows Live ID service
error_codes: ["0x80048821", "0x80048800", "0x8004882E"]
keywords: [sign-in, login, live id, password]
fwlink: 1042
revision: "1.0"
last_review: 2026-08-07
see_also: [300141]
---

## Symptoms

When you add a Windows Live account on the phone, sign-in fails. Depending on the failure, you may
see one of the following:

- The phone reports that your user name or password is incorrect, even though they are correct.
- Sign-in appears to succeed, but no account is added to the phone.
- An error code beginning `8004` is shown.

## Cause

When signing in, you might see one of the following error codes:

| Code | What it means |
|---|---|
| `0x80048821` | The address or password did not match an account. |
| `0x80048800` | The phone asked without being signed in, so there was nothing to check. |
| `0x8004882E` | The phone asked without sending a password at all. |

`0x80048821` usually means you used the wrong email address or password, or the account does not exist
The other two are rarely about your password. They mean the phone tried to access an authenticated 
service without valid credentials, this can indicate the device is in an inconsistent state, or that 
the account is not set up correctly.

## Resolution

If the phone shows `0x80048800` or `0x8004882E`, go straight to step 5. Steps 1 to 4 are about your
account and will not help with those two.

1. **Confirm the account exists.** ReLiveWP accounts are not Microsoft accounts, and you cannot
   sign in with a Microsoft account directly. You must create a ReLiveWP account first, then link
   your Microsoft account to it.

2. **Try signing in on the web.** Sign in [on the ReLiveWP website](https://go.relivewp.net/fwlink/?linkid=1101).
   This tells you where the problem is. If signing in on the web also fails, the problem is with the
   account, so reset your password and try again. If it works, the account is fine and the problem
   is on the phone.

3. **Enter your password on the phone again.** The phone keeps using the password you first gave
   it. If you have changed your password since, the stored one will keep being rejected until you
   replace it.

4. **Check the phone's date and time.** Open **Settings**, then **date+time**. Windows Phone
   normally sets its clock from the mobile network, so a phone used on Wi-Fi only, or one that has
   been switched off for a long time, may have the wrong date and time. Turn **Set automatically**
   off and set the date, time and time zone by hand. Sign-in passes are only valid for a short window,
   so a clock that is wrong will have them rejected as out of date.

5. **Start setup again from the beginning.** `0x80048800` and `0x8004882E` mean the phone tried to
   sign in without valid credentials. Restart the phone and go through account setup from the start
   rather than retrying from the error. If setup then stops and asks for an activation code, see
   [KB300141](/kb/300141).

6. **Factory reset the phone.** If the phone is in an inconsistent state, a factory reset will clear it.
   Open **Settings**, then **about**, then **reset your phone**, or follow your device manufacturer's
   guide to hard reset your device. After the reset, go through account setup again from the start.

## Status

`0x80048821` is by design: a failed credential check is reported the same way Windows Live reported
it, so the phone shows its usual message. The other two codes mean setup did not complete, rather
than that anything is wrong with your account.
