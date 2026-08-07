---
id: 300141
type: PRB
title: Your device asks for an activation code to finish setup.
summary: "The message 'There was a problem setting up your Windows Live ID on the phone and we need an activation code to finish setup' is shown during account setup."
applies_to:
  - Windows Phone 7.0
  - Windows Phone 7.5 (Mango)
  - Windows Phone 7.8
  - ReLiveWP Windows Live ID service
error_codes: []
keywords: [sign-in, login, live id, password, token]
fwlink: 1041
revision: "1.0"
last_review: 2026-08-07
see_also: []
---

## Symptoms

When you add a Windows Live account on the phone, sign-in fails with a message like the following:

```
ACTIVATE WINDOWS LIVE

There was a problem setting up your
Windows Live ID on the phone and we 
need an activation code to finish setup.
You can call customer ervice to get the
code, then enter it here. If you press Skip,
we'll let you know next time you do 
something that requires activation, so you
can call then.
Call support.
```

## Cause

ReLiveWP is in closed beta, as such device activation is intentionally limited to only approved testers. The activation code is a one-time use token that is generated for each approved tester and is required to complete the Windows Live account setup on the phone.

## Resolution

Check your email for an activation code from ReLiveWP. If you are an approved tester and have not received an activation code, contact us to request one.

## Status

This behaviour is by design.