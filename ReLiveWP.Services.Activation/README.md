# ReLiveWP.Services.Activation
Activation server replacement for Windows Phone 7/8.

## What?
This project is a swap in replacement for the Windows Phone activation server 
(dcs.windowsphone.net) which was discontinued some time around 2017. It's job
is to verify a given device is genuine, and if so issue client certificates to 
that device as/when it requests them. These certificates are then used when
communicating with the Marketplace, and Xbox Live. 

## How to use.
In *theory*, using this server is as simple as setting up your local DNS to
point `dcs.windowsphone.net` at it, trusting the generated root certificate
then signing into Windows Live as normal, however for some reason I've not
quite worked out, the activation service won't accept any HTTPS certificate
I've given it, so currently you have to disable HTTPS in the registry.

To do this, in your favourite registry editor navigate to 
`HKEY_LOCAL_MACHINE\Software\Microsoft\GwpCPC` and change the value of 
`ProdEndpoint` to point to the server directly, or to simply use HTTP instead
of HTTPS

After this, you should be able to activate your device, however you won't be
able to login to Windows Live because the domain name used to lookup the client
configuration is no longer valid. 

To fix this, you can add a CNAME record to alias `clientconfig.passport.net` to 
`clientconfig.microsoftonline-p.net`. No certificates or registry tweaks
required! This can also be done via a HTTP proxy like Fiddler relatively easily.

## Current status
 - [x] Support for Windows Phone 7
 - [ ] Support for Windows Phone 8
 - [ ] Require no registry tweaks to activate