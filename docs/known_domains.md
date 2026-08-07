# List of known domains used by Windows Phone 7

## `*.microsoft.com`
### `disco.moservices.microsoft.com`
Windows Phone 7 Discovery Service, used for discovering mobile operators and their services. No longer functional and will need reimplementing.

- `GET /Mobi/Discovery/v1/Operators`: Called with a bunch of device information (including OEM, IMEI, serial number, etc.), unsure what it's meant to return.

## `*.windowsphone.net`
### `dcs.windowsphone.net`
- `POST /dcs/certificaterequest`: Device Provisioning Certificate Request.
    - The device sends a PKCS#10 certificate signing request to the server. The server responds with a PKCS#7 certificate chain containing the device certificate and the root certificate.
    - The device will refuse to connect to any Windows Live services without a valid certificate chain.
    - The original server used this somewhat like Windows Product Activation, where the device would send a product key like string, and the server would validate this accordingly. We're not going to bother with that.
    - Obviously this server is no longer functional, so we'll need to reimplement this to generate our own certificate chain.

## `*.windowsphone.com`
### `marketplaceedgeservice.windowsphone.com`
Functionally equivalent to `catalogue.zune.net`.

### `cdn.marketplaceedgeservice.windowsphone.com`, `cdn.marketplaceimages.windowsphone.com`
Functionally equivalent to `image.catalogue.zune.net`.

### `marketplaceconfigservice.windowsphone.com`
Unsure, possibly equivalent to `tuners.zune.net`.

## `*.passport.net`
### `clientconfig.passport.net`
Windows Live Client Configuration
- `GET /ppcrlconfig600m.bin`: mobile configuration
- `GET /ppcrlconfig600.bin`: desktop configuration

Both of these can be redirected to `https://login.microsoftonline.com/ppcrlconfig600.bin`, which is a signed PE file containing the configuration in XML format. We probably can't touch this directly without modifying the client.

## `*.live.com`
### `login.live.com`
Windows Live Login service.

- `POST /RST2.srf`: Request Security Token, used for authentication on initial login, as well as refreshing tokens and requesting new ones for other services.
- `GET /ppcrlcheck.srf`: Returns the current minimum version of the configuration files.

Both of these are currently perfectly functional but we'll need to reimplement them to support our own authentication system for our own services.

### `directory.services.live.com`
Windows Live Directory service, provides information about people and groups.

- `POST /profile/profile.asmx` - Appears to accept SOAP requests for retrieving profile information. We'll need to reverse engineer this to see what it does.
    - `SOAPAction: "http://profile.live.com/GetMany"`

## `*.hotmail.com`
### `m.hotmail.com`
Windows Live Hotmail service.

- `POST /Microsoft-Server-ActiveSync`: ActiveSync endpoint for Hotmail. Used for email, contacts, and calendar sync. 

## `*.live.net`
### `apis.live.net`
Windows Live API service, provides access to various Windows Live services.

- `POST /Activities`: Appears to be the endpoint for retrieving activity information from the user's social feeds, called when visiting the "what's new" page in the People app. Not sure what data it returns.
    - `/Activities?$format=atom10&$xslt=wp7rafeed&Count=25`.
- `POST /Users(:id)/Status`: called when posting a status update to the user's social feeds.

At time of writing, these APIs return 200 OK but with no content.

### `docs.live.net`
SkyDrive documents, what the Office Hub uses for Word/Excel/PowerPoint/OneNote. Not the same thing as the photo sync in `wmphotos`. See `docs/skydrive-docs.md` for the full protocol.

- `POST /SkyDocsService.svc`: the control plane. This is [MS-STWEB], which is publicly documented, namespace `http://schemas.microsoft.com/clouddocuments`. Operations used by WP7 are `GetWebAccountInfo`, `GetChangesSinceToken`, `GetProductInfo` and `ResolveWebUrl`. SOAPAction is a bare unquoted token, unlike the SharePoint calls in the same binaries.

The host is configurable on device via `HKLM\Software\Microsoft\Office Mobile\SkyDrive\SkyDriveServer`, which is how we redirect it. It is only used to build this SOAP URL.

### `d.docs.live.net`
The WebDAV data plane. Reading only needs `PROPFIND` and `GET`. PROPFIND arrives with no request body, so the server has to return the full property set unprompted, and the property vocabulary is Microsoft's Exchange era one (`D:isFolder`, `D:isreadonly`, `Repl:repl-uid`, `progid`) rather than RFC 4918's.

The repeated `^.Documents` property update is the write path going through `SPSyncCore.dll`, and the following `SkyDocsService.svc` listing is `GetChangesSinceToken`, whose `SyncData` element carries a `D:multistatus` parsed by the same code as a PROPFIND response. That is also where deletions come from, as `HTTP/1.1 404` entries.

This hostname is not fixed. The client takes the DAV host from `RootDavUrl` and the per item `DavUrl` in our own SOAP responses, so we choose it.

### `inference.location.live.net`
Windows Live Location Inference service, provides location information for the user's device, Seems to work sometimes? Usually returns a 403 Forbidden response.

- `POST /inferenceservice/v21/pox/GetLocationUsingFingerprint`: Called with GZIP compressed XML data, not yet looked into it.

## `*.bing.net`
### `appserver.m.bing.net`, `api.m.bing.net`
Bing Mobile specific services, appears completely non-functional (NXDOMAIN). Windows Phone 7 tries to access it when searching in the Bing app.

- `api.m.bing.net/SearchService/Search.svc`: Appears to be the main endpoint for Bing Mobile. Called with JSON data detailing search queries, culture, user parameters, etc.
- `appserver.m.bing.net/ConfigService/ConfigService.svc/restxml/GetRichClientConfigRestXml`: Called to retrieve configuration information for the Bing app, called with a bunch of query parameters including the device model, OS version, etc.
    - `/ConfigService/ConfigService.svc/restxml/GetRichClientConfigRestXml?osName=windows+phone&firstRun=true&osVersion=7.10&culture=en-GB&deviceName=Lumia%20800&AppId=149E786F-EAF3-45a4-B817-9D2E4861D4F6`

## `*.bing.com`
### `api.bing.com`
Standard Bing API, appears to be used only for search suggestions in Internet Explorer, still functional.

- `/qsonhs.aspx`: Called with the search query, returns JSON data containing search suggestions.

## `*.xboxlive.com`
### `activeauth.xboxlive.com`
Xbox Live authentication service, used for Xbox Live games and Xbox Live Extras. Still functional, but would historically rely on certificates provided by `dcs.windowsphone.net` for authentication, therefore will need reimplementing.

- `POST /sts/sts.asmx`: Not sure what STS stands for, called with a `GetSecurityTicket` SOAP request.

## `*.zune.net`
### `catalog.zune.net`, `catalog-ssl.zune.net`
Zune Catalog, used for downloading apps, music, podcasts, etc. No longer functional and will need reimplementing.

### `image.catalog.zune.net`
Zune images CDN. No longer functional and will need reimplementing.

### `tuners.zune.net`, `tuners-ssl.zune.net`
Used for registering devices with the Zune backend. No longer functional and will need reimplementing.

## Misc
- `mangoclient.consumermarketplace.msid.windowsphone.com`: Mentioned in RequestSecurityToken.
- `kdc.xboxlive.com`: Mentioned in RequestSecurityToken.
- `live.xbox.com`: Mentioned in RequestSecurityToken.
- `mobilling.microsoft.com`: Mentioned in RequestSecurityToken.
- `live.zune.net`: Mentioned in RequestSecurityToken.