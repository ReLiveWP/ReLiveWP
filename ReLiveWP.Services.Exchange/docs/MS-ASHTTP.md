**\[MS-ASHTTP\]:**

**Exchange ActiveSync: HTTP Protocol**

Intellectual Property Rights Notice for Open Specifications Documentation

-   **Technical Documentation.** Microsoft publishes Open Specifications documentation ("this documentation") for protocols, file formats, data portability, computer languages, and standards support. Additionally, overview documents cover inter-protocol relationships and interactions.

-   **Copyrights**. This documentation is covered by Microsoft copyrights. Regardless of any other terms that are contained in the terms of use for the Microsoft website that hosts this documentation, you can make copies of it in order to develop implementations of the technologies that are described in this documentation and can distribute portions of it in your implementations that use these technologies or in your documentation as necessary to properly document the implementation. You can also distribute in your implementation, with or without modification, any schemas, IDLs, or code samples that are included in the documentation. This permission also applies to any documents that are referenced in the Open Specifications documentation.

-   **No Trade Secrets**. Microsoft does not claim any trade secret rights in this documentation.

-   **Patents**. Microsoft has patents that might cover your implementations of the technologies described in the Open Specifications documentation. Neither this notice nor Microsoft\'s delivery of this documentation grants any licenses under those patents or any other Microsoft patents. However, a given Open Specifications document might be covered by the Microsoft [Open Specifications Promise](https://go.microsoft.com/fwlink/?LinkId=214445) or the [Microsoft Community Promise](https://go.microsoft.com/fwlink/?LinkId=214448). If you would prefer a written license, or if the technologies described in this documentation are not covered by the Open Specifications Promise or Community Promise, as applicable, patent licenses are available by contacting <iplg@microsoft.com>.

-   **License Programs**. To see all of the protocols in scope under a specific license program and the associated patents, visit the [Patent Map](https://aka.ms/AA9ufj8).

-   **Trademarks**. The names of companies and products contained in this documentation might be covered by trademarks or similar intellectual property rights. This notice does not grant any licenses under those rights. For a list of Microsoft trademarks, visit [www.microsoft.com/trademarks](https://www.microsoft.com/trademarks).

-   **Fictitious Names**. The example companies, organizations, products, domain names, email addresses, logos, people, places, and events that are depicted in this documentation are fictitious. No association with any real company, organization, product, domain name, email address, logo, person, place, or event is intended or should be inferred.

**Reservation of Rights**. All other rights are reserved, and this notice does not grant any rights other than as specifically described above, whether by implication, estoppel, or otherwise.

**Tools**. The Open Specifications documentation does not require the use of Microsoft programming tools or programming environments in order for you to develop an implementation. If you have access to Microsoft programming tools and environments, you are free to take advantage of them. Certain Open Specifications documents are intended for use in conjunction with publicly available standards specifications and network programming art and, as such, assume that the reader either is familiar with the aforementioned material or has immediate access to it.

**Support.** For questions and support, please contact <dochelp@microsoft.com>.

**Revision Summary**

  -------------------------------------------------------------------------------------------------------------------------------
  Date         Revision History   Revision Class   Comments
  ------------ ------------------ ---------------- ------------------------------------------------------------------------------
  12/3/2008    1.0.0              Major            Initial release.

  2/4/2009     1.0.1              Editorial        Revised and edited technical content.

  3/4/2009     1.0.2              Editorial        Revised and edited technical content.

  4/10/2009    2.0.0              Major            Updated technical content and applicable product releases.

  7/15/2009    3.0.0              Major            Revised and edited for technical content.

  11/4/2009    4.0.0              Major            Updated and revised the technical content.

  2/10/2010    5.0.0              Major            Updated and revised the technical content.

  5/5/2010     6.0.0              Major            Updated and revised the technical content.

  8/4/2010     7.0                Major            Significantly changed the technical content.

  11/3/2010    7.1                Minor            Clarified the meaning of the technical content.

  3/18/2011    8.0                Major            Significantly changed the technical content.

  8/5/2011     8.1                Minor            Clarified the meaning of the technical content.

  10/7/2011    8.2                Minor            Clarified the meaning of the technical content.

  1/20/2012    9.0                Major            Significantly changed the technical content.

  4/27/2012    9.1                Minor            Clarified the meaning of the technical content.

  7/16/2012    10.0               Major            Significantly changed the technical content.

  10/8/2012    11.0               Major            Significantly changed the technical content.

  2/11/2013    11.0               None             No changes to the meaning, language, or formatting of the technical content.

  7/26/2013    12.0               Major            Significantly changed the technical content.

  11/18/2013   12.0               None             No changes to the meaning, language, or formatting of the technical content.

  2/10/2014    12.0               None             No changes to the meaning, language, or formatting of the technical content.

  4/30/2014    13.0               Major            Significantly changed the technical content.

  7/31/2014    13.0               None             No changes to the meaning, language, or formatting of the technical content.

  10/30/2014   13.1               Minor            Clarified the meaning of the technical content.

  5/26/2015    14.0               Major            Significantly changed the technical content.

  6/30/2015    15.0               Major            Significantly changed the technical content.

  9/14/2015    16.0               Major            Significantly changed the technical content.

  6/9/2016     17.0               Major            Significantly changed the technical content.

  2/28/2017    18.0               Major            Significantly changed the technical content.

  9/19/2017    19.0               Major            Significantly changed the technical content.

  7/24/2018    20.0               Major            Significantly changed the technical content.

  10/1/2018    21.0               Major            Significantly changed the technical content.

  12/11/2018   21.1               Minor            Clarified the meaning of the technical content.

  4/29/2022    22.0               Major            Significantly changed the technical content.

  5/21/2024    22.1               Minor            Clarified the meaning of the technical content.

  5/20/2025    23.0               Major            Significantly changed the technical content.
  -------------------------------------------------------------------------------------------------------------------------------

Table of Contents

[1 Introduction [7](#introduction)](#introduction)

[1.1 Glossary [7](#glossary)](#glossary)

[1.2 References [9](#references)](#references)

[1.2.1 Normative References [9](#normative-references)](#normative-references)

[1.2.2 Informative References [10](#informative-references)](#informative-references)

[1.3 Overview [10](#overview)](#overview)

[1.4 Relationship to Other Protocols [10](#relationship-to-other-protocols)](#relationship-to-other-protocols)

[1.5 Prerequisites/Preconditions [10](#prerequisitespreconditions)](#prerequisitespreconditions)

[1.6 Applicability Statement [11](#applicability-statement)](#applicability-statement)

[1.7 Versioning and Capability Negotiation [11](#versioning-and-capability-negotiation)](#versioning-and-capability-negotiation)

[1.8 Vendor-Extensible Fields [11](#vendor-extensible-fields)](#vendor-extensible-fields)

[1.9 Standards Assignments [11](#standards-assignments)](#standards-assignments)

[2 Messages [12](#messages)](#messages)

[2.1 Transport [12](#transport)](#transport)

[2.2 Message Syntax [12](#message-syntax)](#message-syntax)

[2.2.1 HTTP POST Request [12](#http-post-request)](#http-post-request)

[2.2.1.1 Request Format [12](#request-format)](#request-format)

[2.2.1.1.1 Request Line [12](#request-line)](#request-line)

[2.2.1.1.1.1 Base64-Encoded Query Value [13](#base64-encoded-query-value)](#base64-encoded-query-value)

[2.2.1.1.1.1.1 Encoded Parameter [14](#encoded-parameter)](#encoded-parameter)

[2.2.1.1.1.1.2 Command Codes [14](#command-codes)](#command-codes)

[2.2.1.1.1.1.3 Command Parameters [15](#command-parameters)](#command-parameters)

[2.2.1.1.1.2 Plain Text Query Value [16](#plain-text-query-value)](#plain-text-query-value)

[2.2.1.1.1.2.1 Command [17](#command)](#command)

[2.2.1.1.1.2.2 User Name [17](#user-name)](#user-name)

[2.2.1.1.1.2.3 Device ID [17](#device-id)](#device-id)

[2.2.1.1.1.2.4 Device type [17](#device-type)](#device-type)

[2.2.1.1.1.2.5 Command-Specific URI Parameters [17](#command-specific-uri-parameters)](#command-specific-uri-parameters)

[2.2.1.1.2 Request Headers [18](#request-headers)](#request-headers)

[2.2.1.1.2.1 Accept-Language [18](#accept-language)](#accept-language)

[2.2.1.1.2.2 Authorization [18](#authorization)](#authorization)

[2.2.1.1.2.3 Content-Type [19](#content-type)](#content-type)

[2.2.1.1.2.4 Cookie [19](#cookie)](#cookie)

[2.2.1.1.2.5 MS-ASAcceptMultiPart [19](#ms-asacceptmultipart)](#ms-asacceptmultipart)

[2.2.1.1.2.6 MS-ASProtocolVersion [20](#ms-asprotocolversion)](#ms-asprotocolversion)

[2.2.1.1.2.7 User-Agent [20](#user-agent)](#user-agent)

[2.2.1.1.2.8 X-MS-PolicyKey [20](#x-ms-policykey)](#x-ms-policykey)

[2.2.1.1.3 Request Body [20](#request-body)](#request-body)

[2.2.2 HTTP POST Response [20](#http-post-response)](#http-post-response)

[2.2.2.1 Response Format [20](#response-format)](#response-format)

[2.2.2.1.1 Status Line [20](#status-line)](#status-line)

[2.2.2.1.2 Response Headers [21](#response-headers)](#response-headers)

[2.2.2.1.2.1 Cache-Control [23](#cache-control)](#cache-control)

[2.2.2.1.2.2 Content-Encoding [23](#content-encoding)](#content-encoding)

[2.2.2.1.2.3 Content-Length [23](#content-length)](#content-length)

[2.2.2.1.2.4 Content-Type [23](#content-type-1)](#content-type-1)

[2.2.2.1.2.5 MS-Server-ActiveSync [23](#ms-server-activesync)](#ms-server-activesync)

[2.2.2.1.2.6 X-MS-Location [23](#x-ms-location)](#x-ms-location)

[2.2.2.1.2.7 MS-ASProtocolCommands [24](#ms-asprotocolcommands)](#ms-asprotocolcommands)

[2.2.2.1.2.8 MS-ASProtocolVersions [24](#ms-asprotocolversions)](#ms-asprotocolversions)

[2.2.2.1.2.9 X-MS-RP [24](#x-ms-rp)](#x-ms-rp)

[2.2.2.1.2.10 X-MS-Credential-Service-Url [24](#x-ms-credential-service-url)](#x-ms-credential-service-url)

[2.2.2.1.2.11 X-MS-Credentials-Expire [24](#x-ms-credentials-expire)](#x-ms-credentials-expire)

[2.2.2.1.2.12 Set-Cookie [24](#set-cookie)](#set-cookie)

[2.2.2.1.2.13 X-MS-ASThrottle [24](#x-ms-asthrottle)](#x-ms-asthrottle)

[2.2.2.1.2.14 X-BEServer [24](#x-beserver)](#x-beserver)

[2.2.2.1.2.15 X-FEServer [25](#x-feserver)](#x-feserver)

[2.2.2.1.2.16 request-id [25](#request-id)](#request-id)

[2.2.2.1.3 Response Body [25](#response-body)](#response-body)

[2.2.3 HTTP OPTIONS Request [25](#http-options-request)](#http-options-request)

[2.2.3.1 Request Format [25](#request-format-1)](#request-format-1)

[2.2.3.1.1 Request Line [25](#request-line-1)](#request-line-1)

[2.2.3.1.2 Request Headers [25](#request-headers-1)](#request-headers-1)

[2.2.4 HTTP OPTIONS Response [25](#http-options-response)](#http-options-response)

[2.2.4.1 Response Format [26](#response-format-1)](#response-format-1)

[2.2.4.1.1 Status Line [26](#status-line-1)](#status-line-1)

[2.2.4.1.2 Response Headers [26](#response-headers-1)](#response-headers-1)

[2.2.4.1.2.1 MS-ASProtocolCommands [26](#ms-asprotocolcommands-1)](#ms-asprotocolcommands-1)

[2.2.4.1.2.2 MS-ASProtocolVersions [26](#ms-asprotocolversions-1)](#ms-asprotocolversions-1)

[2.2.4.1.2.3 Set-Cookie [26](#set-cookie-1)](#set-cookie-1)

[3 Protocol Details [27](#protocol-details)](#protocol-details)

[3.1 Client Details [27](#client-details)](#client-details)

[3.1.1 Abstract Data Model [27](#abstract-data-model)](#abstract-data-model)

[3.1.2 Timers [27](#timers)](#timers)

[3.1.3 Initialization [27](#initialization)](#initialization)

[3.1.4 Higher-Layer Triggered Events [27](#higher-layer-triggered-events)](#higher-layer-triggered-events)

[3.1.4.1 Sending a Command Request [27](#sending-a-command-request)](#sending-a-command-request)

[3.1.5 Message Processing Events and Sequencing Rules [27](#message-processing-events-and-sequencing-rules)](#message-processing-events-and-sequencing-rules)

[3.1.5.1 Handling a Successful Response [27](#handling-a-successful-response)](#handling-a-successful-response)

[3.1.5.2 Handling a Failed Response [28](#handling-a-failed-response)](#handling-a-failed-response)

[3.1.5.2.1 HTTP Error 401, 403, and 500 [28](#http-error-401-403-and-500)](#http-error-401-403-and-500)

[3.1.5.2.2 HTTP Error 451 [28](#http-error-451)](#http-error-451)

[3.1.5.2.3 HTTP Error 503 [28](#http-error-503)](#http-error-503)

[3.1.5.2.4 HTTP Error 456 and 457 [29](#http-error-456-and-457)](#http-error-456-and-457)

[3.1.6 Timer Events [29](#timer-events)](#timer-events)

[3.1.7 Other Local Events [29](#other-local-events)](#other-local-events)

[3.2 Server Details [29](#server-details)](#server-details)

[3.2.1 Abstract Data Model [29](#abstract-data-model-1)](#abstract-data-model-1)

[3.2.2 Timers [29](#timers-1)](#timers-1)

[3.2.3 Initialization [29](#initialization-1)](#initialization-1)

[3.2.4 Higher-Layer Triggered Events [29](#higher-layer-triggered-events-1)](#higher-layer-triggered-events-1)

[3.2.5 Message Processing Events and Sequencing Rules [29](#message-processing-events-and-sequencing-rules-1)](#message-processing-events-and-sequencing-rules-1)

[3.2.5.1 Handling HTTP POST Command Requests [30](#handling-http-post-command-requests)](#handling-http-post-command-requests)

[3.2.5.1.1 User-Agent Change Tracking [30](#user-agent-change-tracking)](#user-agent-change-tracking)

[3.2.5.2 Handling HTTP OPTIONS Command Requests [30](#handling-http-options-command-requests)](#handling-http-options-command-requests)

[3.2.6 Timer Events [31](#timer-events-1)](#timer-events-1)

[3.2.7 Other Local Events [31](#other-local-events-1)](#other-local-events-1)

[4 Protocol Examples [32](#protocol-examples)](#protocol-examples)

[4.1 FolderSync Request and Response [32](#foldersync-request-and-response)](#foldersync-request-and-response)

[4.2 FolderSync Request and Redirect Response [32](#foldersync-request-and-redirect-response)](#foldersync-request-and-redirect-response)

[4.3 HTTP OPTIONS Command Request and Response [33](#http-options-command-request-and-response)](#http-options-command-request-and-response)

[4.4 SendMail Request and Response [33](#sendmail-request-and-response)](#sendmail-request-and-response)

[4.5 CreateFolder Request and Response [34](#createfolder-request-and-response)](#createfolder-request-and-response)

[5 Security [36](#security)](#security)

[5.1 Security Considerations for Implementers [36](#security-considerations-for-implementers)](#security-considerations-for-implementers)

[5.2 Index of Security Parameters [36](#index-of-security-parameters)](#index-of-security-parameters)

[6 Appendix A: Product Behavior [37](#appendix-a-product-behavior)](#appendix-a-product-behavior)

[7 Change Tracking [39](#change-tracking)](#change-tracking)

[8 Index [40](#index)](#index)

# Introduction

The Exchange ActiveSync: HTTP Protocol enables client devices to synchronize data with the data that is stored on the server.

Sections 1.5, 1.8, 1.9, 2, and 3 of this specification are normative. All other sections and examples in this specification are informative.

## Glossary

This document uses the following terms:

> []{#gt_d046b6e2-3f79-47e1-87d7-754566744dcd .anchor}**alias**: An alternate name that can be used to reference an object or element.
>
> []{#gt_24ddbbb4-b79e-4419-96ec-0fdd229c9ebf .anchor}**Augmented Backus-Naur Form (ABNF)**: A modified version of Backus-Naur Form (BNF), commonly used by Internet specifications. ABNF notation balances compactness and simplicity with reasonable representational power. ABNF differs from standard BNF in its definitions and uses of naming rules, repetition, alternatives, order-independence, and value ranges. For more information, see [\[RFC5234\]](https://go.microsoft.com/fwlink/?LinkId=123096).
>
> []{#gt_179b9392-9019-45a3-880b-26f6890522b7 .anchor}**base64 encoding**: A binary-to-text encoding scheme whereby an arbitrary sequence of bytes is converted to a sequence of printable ASCII characters, as described in [\[RFC4648\]](https://go.microsoft.com/fwlink/?LinkId=90487).
>
> []{#gt_7204b2ed-dcef-4434-be15-6451f92d03fb .anchor}**calendar**: A date range that shows availability, [**meetings**](#gt_cbc56efc-e4f7-4b31-9e5f-9c44e3924d94), and appointments for one or more users or resources. See also Calendar object.
>
> []{#gt_48d3e923-3081-4b1c-a8b4-db07cc022128 .anchor}**contact**: (1) A presence entity (presentity) whose presence information can be tracked.
>
> \(2\) An object of the contact class that represents a company or person whom a user can contact.
>
> []{#gt_e595b084-31d1-4211-9d4a-f4671957413c .anchor}**encrypted message**: An Internet email message that is in the format described by [\[RFC5751\]](https://go.microsoft.com/fwlink/?LinkID=194261) and uses the EnvelopedData CMS content type described in [\[RFC3852\]](https://go.microsoft.com/fwlink/?LinkId=90445), or the [**Message object**](#gt_b6c15d0c-d992-421d-ba96-99d3b63894cf) that represents such a message.
>
> []{#gt_6fbe9d37-508e-44f3-be0f-b579e1264f27 .anchor}**Global Address List (GAL)**: An address list that conceptually represents the default address list for an address book.
>
> []{#gt_f49694cc-c350-462d-ab8e-816f0103c6c1 .anchor}**globally unique identifier (GUID)**: A term used interchangeably with universally unique identifier (UUID) in Microsoft protocol technical documents (TDs). Interchanging the usage of these terms does not imply or require a specific algorithm or mechanism to generate the value. Specifically, the use of this term does not imply or require that the algorithms described in [\[RFC4122\]](https://go.microsoft.com/fwlink/?LinkId=90460) or [\[C706\]](https://go.microsoft.com/fwlink/?LinkId=89824) have to be used for generating the GUID. See also universally unique identifier (UUID).
>
> []{#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd .anchor}**Hypertext Transfer Protocol (HTTP)**: An application-level protocol for distributed, collaborative, hypermedia information systems (text, graphic images, sound, video, and other multimedia files) on the World Wide Web.
>
> []{#gt_9239bd88-9747-44a6-83a6-473f53f175a7 .anchor}**Hypertext Transfer Protocol Secure (HTTPS)**: An extension of HTTP that securely encrypts and decrypts web page requests. In some older protocols, \"Hypertext Transfer Protocol over Secure Sockets Layer\" is still used (Secure Sockets Layer has been deprecated). For more information, see [\[SSL3\]](https://go.microsoft.com/fwlink/?LinkId=90534) and [\[RFC5246\]](https://go.microsoft.com/fwlink/?LinkId=129803).
>
> []{#gt_baa08600-0402-47f6-a8ce-9690cf962c96 .anchor}**Inbox folder**: A special folder that is the default location for [**Message objects**](#gt_b6c15d0c-d992-421d-ba96-99d3b63894cf) received by a user or resource.
>
> []{#gt_7b78ebef-e35d-45ab-abfd-4121b60995de .anchor}**locale**: A collection of rules and data that are specific to a language and a geographical area. A locale can include information about sorting rules, date and time formatting, numeric and monetary conventions, and character classification.
>
> []{#gt_d3ad0e15-adc9-4174-bacf-d929b57278b3 .anchor}**mailbox**: A message store that contains email, calendar items, and other [**Message objects**](#gt_b6c15d0c-d992-421d-ba96-99d3b63894cf) for a single recipient.
>
> []{#gt_cbc56efc-e4f7-4b31-9e5f-9c44e3924d94 .anchor}**meeting**: An event with attendees.
>
> []{#gt_85d4db24-1560-4ac1-aa9b-6cd96f36c0e0 .anchor}**meeting request**: An instance of a Meeting Request object.
>
> []{#gt_b6c15d0c-d992-421d-ba96-99d3b63894cf .anchor}**Message object**: A set of properties that represents an email message, appointment, contact, or other type of personal-information-management object. In addition to its own properties, a Message object contains recipient properties that represent the addressees to which it is addressed, and an attachments table that represents any files and other Message objects that are attached to it.
>
> []{#gt_bcc83734-de00-4cd2-a344-4455ac688da9 .anchor}**MIME message**: A message that is as described in [\[RFC2045\]](https://go.microsoft.com/fwlink/?LinkId=90307), [\[RFC2046\]](https://go.microsoft.com/fwlink/?LinkId=90308), and [\[RFC2047\]](https://go.microsoft.com/fwlink/?LinkId=90309).
>
> []{#gt_cb5ce626-9f44-4967-b6f5-cc58163db5f9 .anchor}**OAuth**: The OAuth 2.0 authorization framework [\[RFC6749\]](https://go.microsoft.com/fwlink/?LinkId=301486).
>
> []{#gt_d4ab6719-b583-467a-a631-95feb7a5ea34 .anchor}**Out of Office (OOF)**: One of the possible values for the free/busy status on an appointment. It indicates that the user will not be in the office during the appointment.
>
> []{#gt_afa1b8ad-29c4-4f4a-90ce-e63b3547e15a .anchor}**plain text**: Text that does not have markup. See also plain text message body.
>
> []{#gt_53dfe4f3-05d0-41aa-8217-ecd1962b340b .anchor}**recipient**: An entity that can receive email messages.
>
> []{#gt_84bfada5-a327-4110-a257-cffd8fc3fe61 .anchor}**S/MIME (Secure/Multipurpose Internet Mail Extensions)**: A set of cryptographic security services, as described in \[RFC5751\].
>
> []{#gt_d7ef66a9-f154-4d88-bda9-98bdf7235352 .anchor}**Secure Sockets Layer (SSL)**: A security protocol that supports confidentiality and integrity of messages in client and server applications that communicate over open networks. SSL supports server and, optionally, client authentication using X.509 certificates [\[X509\]](https://go.microsoft.com/fwlink/?LinkId=90590) and [\[RFC5280\]](https://go.microsoft.com/fwlink/?LinkId=131034). SSL is superseded by [**Transport Layer Security (TLS)**](#gt_f2bc7fed-7e02-4fa5-91b3-97f5c978563a). TLS version 1.0 is based on SSL version 3.0 \[SSL3\].
>
> []{#gt_fe856661-83ad-4264-85d4-f4c4fa4ce2cb .anchor}**Sent Items folder**: A special folder that is the default location for storing copies of [**Message objects**](#gt_b6c15d0c-d992-421d-ba96-99d3b63894cf) after they are submitted or sent.
>
> []{#gt_3cc9da36-889e-4dc8-9c51-579d091665ae .anchor}**server ID**: A unique identifier that is assigned by the server to each object that can be synchronized. A client stores the server ID for each object and is able to locate an object when given a server ID.
>
> []{#gt_f17e8628-fadb-462b-a654-8ad363204d17 .anchor}**SSL/TLS handshake**: The process of negotiating and establishing a connection protected by [**Secure Sockets Layer (SSL)**](#gt_d7ef66a9-f154-4d88-bda9-98bdf7235352) or [**Transport Layer Security (TLS)**](#gt_f2bc7fed-7e02-4fa5-91b3-97f5c978563a). For more information, see \[SSL3\] and [\[RFC2246\]](https://go.microsoft.com/fwlink/?LinkId=90324).
>
> []{#gt_f2bc7fed-7e02-4fa5-91b3-97f5c978563a .anchor}**Transport Layer Security (TLS)**: A security protocol that supports confidentiality and integrity of messages in client and server applications communicating over open networks. TLS supports server and, optionally, client authentication by using X.509 certificates (as specified in \[X509\]). TLS is standardized in the IETF TLS working group.
>
> []{#gt_e18af8e8-01d7-4f91-8a1e-0fb21b191f95 .anchor}**Uniform Resource Identifier (URI)**: A string that identifies a resource. The URI is an addressing mechanism defined in Internet Engineering Task Force (IETF) Uniform Resource Identifier (URI): Generic Syntax [\[RFC3986\]](https://go.microsoft.com/fwlink/?LinkId=90453).
>
> []{#gt_433a4fb7-ef84-46b0-ab65-905f5e3a80b1 .anchor}**Uniform Resource Locator (URL)**: A string of characters in a standardized format that identifies a document or resource on the World Wide Web. The format is as specified in [\[RFC1738\]](https://go.microsoft.com/fwlink/?LinkId=90287).
>
> []{#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc .anchor}**Wireless Application Protocol (WAP) Binary XML (WBXML)**: A compact binary representation of [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) that is designed to reduce the transmission size of XML documents over narrowband communication channels.
>
> []{#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85 .anchor}**XML**: The Extensible Markup Language, as described in [\[XML1.0\]](https://go.microsoft.com/fwlink/?LinkId=90599).
>
> []{#gt_c7e91c99-e45a-44c2-a08a-c34f137a2cae .anchor}**XML schema definition (XSD)**: The World Wide Web Consortium (W3C) standard language that is used in defining XML schemas. Schemas are useful for enforcing structure and constraining the types of data that can be used validly within other XML documents. XML schema definition refers to the fully specified and currently recommended standard for use in authoring XML schemas.
>
> **MAY, SHOULD, MUST, SHOULD NOT, MUST NOT:** These terms (in all caps) are used as defined in [\[RFC2119\]](https://go.microsoft.com/fwlink/?LinkId=90317). All statements of optional behavior use either MAY, SHOULD, or SHOULD NOT.

## References

Links to a document in the Microsoft Open Specifications library point to the correct section in the most recently published version of the referenced document. However, because individual documents in the library are not updated at the same time, the section numbers in the documents may not match. You can confirm the correct section numbering by checking the [Errata](https://go.microsoft.com/fwlink/?linkid=850906).

### Normative References

We conduct frequent surveys of the normative references to assure their continued availability. If you have any issue with finding a normative reference, please contact <dochelp@microsoft.com>. We will assist you in finding the relevant information.

\[MS-ASCMD\] Microsoft Corporation, \"[Exchange ActiveSync: Command Reference Protocol](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a)\".

\[MS-ASPROV\] Microsoft Corporation, \"[Exchange ActiveSync: Provisioning Protocol](%5bMS-ASPROV%5d.pdf#Section_449c453b74d74919bfe895972b27048a)\".

\[MS-LCID\] Microsoft Corporation, \"[Windows Language Code Identifier (LCID) Reference](%5bMS-LCID%5d.pdf#Section_70feba9f294e491eb6eb56532684c37f)\".

\[RFC1945\] Berners-Lee, T., Fielding, R., and Frystyk, H., \"Hypertext Transfer Protocol \-- HTTP/1.0\", RFC 1945, May 1996, [https://www.rfc-editor.org/info/rfc1945](https://go.microsoft.com/fwlink/?LinkId=90300)

\[RFC2045\] Freed, N., and Borenstein, N., \"Multipurpose Internet Mail Extensions (MIME) Part One: Format of Internet Message Bodies\", RFC 2045, November 1996, [https://www.rfc-editor.org/info/rfc2045](https://go.microsoft.com/fwlink/?LinkId=90307)

\[RFC2119\] Bradner, S., \"Key words for use in RFCs to Indicate Requirement Levels\", BCP 14, RFC 2119, March 1997, [https://www.rfc-editor.org/info/rfc2119](https://go.microsoft.com/fwlink/?LinkId=90317)

\[RFC2616\] Fielding, R., Gettys, J., Mogul, J., et al., \"Hypertext Transfer Protocol \-- HTTP/1.1\", RFC 2616, June 1999, [https://www.rfc-editor.org/info/rfc2616](https://go.microsoft.com/fwlink/?LinkId=90372)

\[RFC2818\] Rescorla, E., \"HTTP Over TLS\", RFC 2818, May 2000, [https://www.rfc-editor.org/info/rfc2818](https://go.microsoft.com/fwlink/?LinkId=90383)

\[RFC2822\] Resnick, P., Ed., \"Internet Message Format\", RFC 2822, April 2001, [https://www.rfc-editor.org/info/rfc2822](https://go.microsoft.com/fwlink/?LinkId=90385)

\[RFC3280\] Housley, R., Polk, W., Ford, W., and Solo, D., \"Internet X.509 Public Key Infrastructure Certificate and Certificate Revocation List (CRL) Profile\", RFC 3280, April 2002, [http://www.rfc-editor.org/info/rfc3280](https://go.microsoft.com/fwlink/?LinkId=90414)

\[RFC4985\] Santesson, S., \"Internet X.509 Public Key Infrastructure Subject Alternative Name for Expression of Service Name\", RFC 4985, August 2007, [http://www.rfc-editor.org/rfc/rfc4985.txt](https://go.microsoft.com/fwlink/?LinkId=193320)

\[RFC5234\] Crocker, D., Ed., and Overell, P., \"Augmented BNF for Syntax Specifications: ABNF\", STD 68, RFC 5234, January 2008, [https://www.rfc-editor.org/info/rfc5234](https://go.microsoft.com/fwlink/?LinkId=123096)

\[RFC5246\] Dierks, T., and Rescorla, E., \"The Transport Layer Security (TLS) Protocol Version 1.2\", RFC 5246, August 2008, [https://www.rfc-editor.org/info/rfc5246](https://go.microsoft.com/fwlink/?LinkId=129803)

\[RFC6265\] Barth, A., \"HTTP State Management Mechanism\", RFC 6265, April 2011, [https://tools.ietf.org/html/rfc6265](https://go.microsoft.com/fwlink/?LinkId=523842)

\[RFC6749\] Hardt, D., Ed., \"The OAuth 2.0 Authorization Framework\", RFC 6749, October 2012, [https://www.rfc-editor.org/info/rfc6749](https://go.microsoft.com/fwlink/?LinkId=301486)

\[WBXML1.2\] Martin, B., and Jano, B., Eds., \"WAP Binary XML Content Format\", W3C Note, June 1999, [http://www.w3.org/1999/06/NOTE-wbxml-19990624](https://go.microsoft.com/fwlink/?LinkId=160492)

### Informative References

\[MS-OXPROTO\] Microsoft Corporation, \"[Exchange Server Protocols System Overview](%5bMS-OXPROTO%5d.pdf#Section_734ab967e43e425babe1974af56c0283)\".

\[MSDN-APM\] Marquardt, T., \"ASP.NET Performance Monitoring, and When to Alert Administrators\", [http://msdn.microsoft.com/en-us/library/ms972959.aspx](https://go.microsoft.com/fwlink/?LinkId=122592)

## Overview

This protocol is used to synchronize server data with a client mobile device. The protocol relies on a client/server architecture. In this specification, the term \"client\" is used to refer to the software that is running on the device and communicating to the server by means of the ActiveSync protocol. The term \"server\" refers to the synchronization engine that communicates the synchronization protocol to the client.

All communication between the client and server is initiated by the client and is based on request/response messages. When the client communicates with the server, the client sends a request to the server as an **[HTTP](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) POST** method, using UTF-8 encoding. The server sends back a response to the HTTP **POST**. The request and response each have a start-line, headers, and might have a body. The format is dictated by the HTTP/1.1 standard. The HTTP **POST** request header contains certain parameters that are set by the client, as specified later in this document. The HTTP **POST** response header is created by the server, and its contents are specified later in this document. The format of the body for both request and response depends on the type of request. Generally, the request/response body contains [**Wireless Application Protocol (WAP) Binary XML (WBXML)**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc) formatted data. Each HTTP **POST** request contains a single command, such as the **Sync** command. A typical session includes several commands and, therefore, several HTTP **POST** requests.

In addition to the HTTP **POST** request/response commands, the HTTP **OPTIONS** command response provides the supported ActiveSync capabilities of the server, including supported commands and supported protocol versions.

## Relationship to Other Protocols

This protocol uses either an [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) connection or an [**HTTPS**](#gt_9239bd88-9747-44a6-83a6-473f53f175a7) connection between the client and server. A TCP/IP network transports messages between a client and server by using either the HTTP protocol or the HTTPS protocol, by means of a series of request and response calls. The protocol specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) uses this protocol as a transport.

For conceptual background information and overviews of the relationships and interactions between this and other protocols, see [\[MS-OXPROTO\]](%5bMS-OXPROTO%5d.pdf#Section_734ab967e43e425babe1974af56c0283).

## Prerequisites/Preconditions

This protocol assumes that authentication has been performed by the underlying protocols.

## Applicability Statement

This protocol specifies the transport mechanism for the commands defined in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) and all data structures associated with those commands. It is applicable to any client or server that synchronizes [**calendar**](#gt_7204b2ed-dcef-4434-be15-6451f92d03fb), [**contact (2)**](#gt_48d3e923-3081-4b1c-a8b4-db07cc022128), e-mail, task, note, and other data between a mail server and a mobile device.

## Versioning and Capability Negotiation

The **[HTTP](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) OPTIONS** command (section [2.2.3](#Section_a8e2fea0b9c14141b9bb2f8d38fbe76c)) is used by the client to discover which versions of the ActiveSync protocol are supported by the server. To determine the supported versions, the client examines the MS-ASProtocolVersions header (section [2.2.4.1.2.2](#Section_6ba4775d24c94132b30e1f86b1b3b868)), which is returned in the HTTP **OPTIONS** command response.

The client uses the MS-ASProtocolVersion header (section [2.2.1.1.2.6](#Section_53676b94410d48bbb92b7a88e1667dd9)) of the HTTP **POST** command (section [2.2.1](#Section_0eee39a74c4d450f89ad4d9eb74bc60e)) to indicate to the server which ActiveSync protocol version it is using.

The latest version of the ActiveSync protocol that the client or server can support is 16.1. Older versions include 16.0, 14.1, 14.0, 12.1, 12.0, and 2.5. Some commands and functionality described in the ActiveSync protocol documentation are not supported by all of the protocol versions. See the command and element descriptions in the ActiveSync protocol documents to determine which commands, elements, and capabilities are supported by the protocol versions.

## Vendor-Extensible Fields

None

## Standards Assignments

None

# Messages

## Transport

Messages are transported by using [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) **POST** and HTTP **OPTIONS**, as specified in [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372). These commands are sent via HTTP or [**Hypertext Transfer Protocol over Secure Sockets Layer (HTTPS)**](#gt_9239bd88-9747-44a6-83a6-473f53f175a7). The query parameters in the request [**URI**](#gt_e18af8e8-01d7-4f91-8a1e-0fb21b191f95) can be encoded with [**base64 encoding**](#gt_179b9392-9019-45a3-880b-26f6890522b7) (see section [2.2.1.1.1.1](#Section_9f75a516edff48d294b95f2a3cc570d3)) or in plain text (see section [2.2.1.1.1.2](#Section_665d4e62c72c481e8015b4dbc7fb8738)). The body of the HTTP message contains the [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc) that is required by the command being communicated in the message. The commands are specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

## Message Syntax

The XML markup that constitutes the request body (section [2.2.1.1.3](#Section_611df21bacb74a629cdb0f8018bc9497)) or the response body (section [2.2.2.1.3](#Section_387ee63b05b648d5b1e8e0130fe0e7d2)) is transmitted between client and server by using [**Wireless Application Protocol (WAP) Binary XML (WBXML)**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc), as specified in [\[WBXML1.2\]](https://go.microsoft.com/fwlink/?LinkId=160492).

The following are the two general types of messages:

-   [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) **POST**

-   HTTP **OPTIONS**

### HTTP POST Request

The client creates a request by using the **[HTTP](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) POST** command to initiate communications between the client and the server.

#### Request Format

Each command is sent from the client to the server as an **[HTTP](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) POST** containing command data. As specified by [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372), the format is as follows.

1.  Request-line

    Request-headers

    CR/LF

    Request Body

##### Request Line

The request line consists of the method indicator, **POST**, followed by the [**URI**](#gt_e18af8e8-01d7-4f91-8a1e-0fb21b191f95), followed by the [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) version, as follows.

5.  POST \<URI\> HTTP/1.1

The URI can be either an absolute URI or a relative URI, as specified in [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372) section 3.2.1. The absolute URI consists of a scheme indicator, the host name, and the path, followed by a query value. The relative URI consists of the path and the query value.

The path and query value in the URI have the following format.

6.  /\<ActiveSync virtual directory name\>?\<query value\>

The query value in the URI contains all of the URI parameters and can contain some of the request headers. The format can be either [**plain text**](#gt_afa1b8ad-29c4-4f4a-90ce-e63b3547e15a), as specified in section [2.2.1.1.1.2](#Section_665d4e62c72c481e8015b4dbc7fb8738), or [**base64 encoding**](#gt_179b9392-9019-45a3-880b-26f6890522b7), as specified in section [2.2.1.1.1.1](#Section_9f75a516edff48d294b95f2a3cc570d3). Either format can be used with protocol versions 12.1, 14.0, 14.1, 16.0, and 16.1. The base64 encoding format is not supported by protocol versions 2.5 and 12.0; the plain text format is supported by all protocol versions.

The following two examples are equivalent. The first example uses the plain text query value, and the second example uses the base64-encoded query value.

7.  POST /Microsoft-Server-ActiveSync?Cmd=Sync&User=rmjones&DeviceId=v140Device&DeviceType=SmartPhone HTTP/1.1

    Content-Type: application/vnd.ms-sync.wbxml

    MS-ASProtocolVersion: 14.0

    User-Agent: ASOM

    Host: Contoso.com

    Accept-Language: en-us

    Content-Length: 868

    POST /Microsoft-Server-ActiveSync?jAAJBAp2MTQwRGV2aWNlAApTbWFydFBob25l HTTP/1.1

    Content-Type: application/vnd.ms-sync.wbxml

    User-Agent: ASOM

    Host: Contoso.com

    Content-Length: 866

###### Base64-Encoded Query Value

The base64-encoded query value uses [**base64 encoding**](#gt_179b9392-9019-45a3-880b-26f6890522b7) to specify the [**URI**](#gt_e18af8e8-01d7-4f91-8a1e-0fb21b191f95) parameters and request headers. The URI parameters and request headers are contained within the fields of a byte sequence. Once the byte sequence is created, it is converted to base64 as specified in [\[RFC2045\]](https://go.microsoft.com/fwlink/?LinkId=90307). The base64-encoded query value is then appended to the request URI.

The following is an example of a URI that contains a base64-encoded query value.

20. /Microsoft-Server-ActiveSync?jAAJBAp2MTQwRGV2aWNlAApTbWFydFBob25l

**NOTE:** The base64-encoded query value is supported only by protocol versions 12.1, 14.0, 14.1, 16.0, and 16.1. If the client uses protocol version 2.5 or 12.0, the plain text query value, as specified in section [2.2.1.1.1.2](#Section_665d4e62c72c481e8015b4dbc7fb8738), MUST be used in the request URI.

The fields of the byte sequence are as follows.

<table>
<colgroup>
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
</colgroup>
<thead>
<tr class="header">
<th>0</th>
<th>1</th>
<th>2</th>
<th>3</th>
<th>4</th>
<th>5</th>
<th>6</th>
<th>7</th>
<th>8</th>
<th>9</th>
<th><p>1</p>
<p>0</p></th>
<th>1</th>
<th>2</th>
<th>3</th>
<th>4</th>
<th>5</th>
<th>6</th>
<th>7</th>
<th>8</th>
<th>9</th>
<th><p>2</p>
<p>0</p></th>
<th>1</th>
<th>2</th>
<th>3</th>
<th>4</th>
<th>5</th>
<th>6</th>
<th>7</th>
<th>8</th>
<th>9</th>
<th><p>3</p>
<p>0</p></th>
<th>1</th>
</tr>
</thead>
<tbody>
<tr class="odd">
<td colspan="8">Protocol version</td>
<td colspan="8">Command code</td>
<td colspan="16">Locale</td>
</tr>
<tr class="even">
<td colspan="8">Device ID length</td>
<td colspan="24">Device ID (variable)</td>
</tr>
<tr class="odd">
<td colspan="32">...</td>
</tr>
<tr class="even">
<td colspan="8">Policy key length</td>
<td colspan="24">Policy key (optional)</td>
</tr>
<tr class="odd">
<td colspan="8">...</td>
<td colspan="8">Device type length</td>
<td colspan="16">Device type (variable)</td>
</tr>
<tr class="even">
<td colspan="32">...</td>
</tr>
<tr class="odd">
<td colspan="32">Command parameters (variable)</td>
</tr>
<tr class="even">
<td colspan="32">...</td>
</tr>
</tbody>
</table>

**Protocol version (1 byte):** An integer that specifies the version of the ActiveSync protocol that is being used. This value SHOULD[\<1\>](\l) be 141, 160 or 161. This value MAY[\<2\>](\l) be 140 or 121.

**Command code (1 byte):** An integer that specifies the command (see table of command codes in section [2.2.1.1.1.1.2](#Section_0ab55ebc6ea94ae4af375736d5195d46)).

**Locale (2 bytes):** An integer that specifies the [**locale**](#gt_7b78ebef-e35d-45ab-abfd-4121b60995de) of the language that is used for the response. Locale integer values are specified in [\[MS-LCID\]](%5bMS-LCID%5d.pdf#Section_70feba9f294e491eb6eb56532684c37f).

**Device ID length (1 byte):** An integer that specifies the length of the **Device ID** field. This value MUST be greater than 0.

**Device ID (variable):** A string or a [**GUID**](#gt_f49694cc-c350-462d-ab8e-816f0103c6c1) that identifies the device. For details, see section [2.2.1.1.1.2.3](#Section_beecc86b75ae47ecb92ae9d20edac80c). The length of this field is specified by the **Device ID length** field.

**Policy key length (1 byte):** An integer that specifies the length of the policy key. The only valid values are 0 or 4. A value of 0 indicates that the policy key field is absent.

**Policy key (4 bytes, optional):** An unsigned integer that indicates the state of policy settings on the client device, as specified in [\[MS-ASPROV\]](%5bMS-ASPROV%5d.pdf#Section_449c453b74d74919bfe895972b27048a) section 2.2.2.42. If the value of the **Policy key length** field is 0, this field is absent.

**Device type length (1 byte):** An integer that specifies the length of the **Device type** field.

**Device type (variable):** A string that specifies the type of client device. For details, see section [2.2.1.1.1.2.4](#Section_6a4407cdf6004f9dba499bdfef765a8d). The length of this field is specified by the **Device type length** field.

**Command parameters (variable):** An array of **Encoded Parameter** structures as specified in section [2.2.1.1.1.1.1](#Section_f94209e9236e4fb9911e9c1571ecafd2). This field is only present if there are command-specific parameters associated with the command specified by the **Command code** field. See section [2.2.1.1.1.1.3](#Section_49dda9263e7e4b6fa5c358630d870a47) for a list of command-specific parameters.

####### Encoded Parameter

<table>
<colgroup>
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
<col style="width: 3%" />
</colgroup>
<thead>
<tr class="header">
<th>0</th>
<th>1</th>
<th>2</th>
<th>3</th>
<th>4</th>
<th>5</th>
<th>6</th>
<th>7</th>
<th>8</th>
<th>9</th>
<th><p>1</p>
<p>0</p></th>
<th>1</th>
<th>2</th>
<th>3</th>
<th>4</th>
<th>5</th>
<th>6</th>
<th>7</th>
<th>8</th>
<th>9</th>
<th><p>2</p>
<p>0</p></th>
<th>1</th>
<th>2</th>
<th>3</th>
<th>4</th>
<th>5</th>
<th>6</th>
<th>7</th>
<th>8</th>
<th>9</th>
<th><p>3</p>
<p>0</p></th>
<th>1</th>
</tr>
</thead>
<tbody>
<tr class="odd">
<td colspan="8">Tag</td>
<td colspan="8">Length</td>
<td colspan="16">Value (variable)</td>
</tr>
<tr class="even">
<td colspan="32">...</td>
</tr>
</tbody>
</table>

**Tag (1 byte):** An integer that identifies the parameter. See section [2.2.1.1.1.1.3](#Section_49dda9263e7e4b6fa5c358630d870a47) for a list of tags and their corresponding parameters.

**Length (1 byte):** An integer that specifies the length of the parameter value. Valid values are from 0 to 255 characters.

**Value (variable):** The value of the parameter. The size of this field is specified by the **Length** field.

####### Command Codes

The following table provides the numeric codes that correspond to the ActiveSync commands. The numeric code is used in the **Command code** field of the base64 encoded [**URI**](#gt_e18af8e8-01d7-4f91-8a1e-0fb21b191f95) to specify the command. For more details, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

  ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Code   Command                 Description
  ------ ----------------------- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  0      **Sync**                Synchronizes changes in a folder between the client and the server.

  1      **SendMail**            Sends mail to the server. This command is issued in the **[HTTP](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) POST** command\'s URI, and does not contain an XML body. The body will instead contain the [**MIME message**](#gt_bcc83734-de00-4cd2-a344-4455ac688da9).

  2      **SmartForward**        Forwards a [**Message object**](#gt_b6c15d0c-d992-421d-ba96-99d3b63894cf) without retrieving the full Message object from the server.

  3      **SmartReply**          Replies to a Message object without retrieving the full Message object from the server.

  4      **GetAttachment**       Retrieves an e-mail attachment from the server.

  9      **FolderSync**          Synchronizes the folder hierarchy but does not synchronize the items in the folders.

  10     **FolderCreate**        Creates an e-mail, [**calendar**](#gt_7204b2ed-dcef-4434-be15-6451f92d03fb), or [**contacts**](#gt_48d3e923-3081-4b1c-a8b4-db07cc022128) folder on the server.

  11     **FolderDelete**        Deletes a folder from the server.

  12     **FolderUpdate**        Moves a folder from one location to another on the server and is used to rename folders.

  13     **MoveItems**           Moves items from one folder to another.

  14     **GetItemEstimate**     Gets an estimate of the number of items in a folder that is synchronized.

  15     **MeetingResponse**     Used to accept, tentatively accept, or decline a [**meeting request**](#gt_85d4db24-1560-4ac1-aa9b-6cd96f36c0e0) in the user\'s [**Inbox folder**](#gt_baa08600-0402-47f6-a8ce-9690cf962c96).

  16     **Search**              Finds and retrieves information about contacts (2) and [**recipients**](#gt_53dfe4f3-05d0-41aa-8217-ecd1962b340b) in the [**Global Address List**](#gt_6fbe9d37-508e-44f3-be0f-b579e1264f27).

  17     **Settings**            Supports getting and setting global properties, such as [**Out of Office (OOF)**](#gt_d4ab6719-b583-467a-a631-95feb7a5ea34) and device information.

  18     **Ping**                Requests that the server monitor specified folders for changes that would require the client to resynchronize.

  19     **ItemOperations**      Identifies the body of the request or response as containing a set of commands operating on items.

  20     **Provision**           Gets the security policy settings set by the server administrator, such as the user\'s minimum password length requirement.

  21     **ResolveRecipients**   Resolves a list of supplied recipients and optionally fetches their [**S/MIME**](#gt_84bfada5-a327-4110-a257-cffd8fc3fe61) certificates so that clients can send [**encrypted messages**](#gt_e595b084-31d1-4211-9d4a-f4671957413c).

  22     **ValidateCert**        Validates a certificate that has been received through an S/MIME mail.

  23     **Find**                Searches for items in the mailbox using KQL syntax.
  ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

####### Command Parameters

The following table lists the tag values that correspond to the names of the command parameters. For additional details about the **AttachmentName**, **CollectionId**, **ItemId**, **LongId**, and **Occurrence** command parameters, see section [2.2.1.1.1.2.5](#Section_f098fd5c0be547279674c25ec54083c3).

  -----------------------------------------------------------------------
  Tag              Parameter Name
  ---------------- ------------------------------------------------------
  0                **AttachmentName**

  1                **CollectionId**

  3                **ItemId**

  4                **LongId**

  6                **Occurrence**

  7                **Options**

  8                **User**
  -----------------------------------------------------------------------

The following table describes the **Options** and **User** command parameters.

  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Parameter     Description                                                                                                   Used By
  ------------- ------------------------------------------------------------------------------------------------------------- --------------------------------------------------------------------
  **Options**   A single-byte bitmask that specifies command options. See the table below for valid flags for this bitmask.   **SmartReply**, **SmartForward**, **SendMail**, **ItemOperations**

  **User**      A string that specifies the user ID in a format that can be logged in the Web server log.                     Any command
  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

The following table specifies the valid bit flags for the **Options** parameter.

  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Flag                  Value    Meaning
  --------------------- -------- -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  **SaveInSent**        0x01     Set this flag to instruct the server to save the [**Message object**](#gt_b6c15d0c-d992-421d-ba96-99d3b63894cf) in the user\'s [**Sent Items folder**](#gt_fe856661-83ad-4264-85d4-f4c4fa4ce2cb). Valid for **SendMail**, **SmartForward**, and **SmartReply**.

  **AcceptMultiPart**   0x02     Set this flag to instruct the server to return the requested item in multipart format. Valid for **ItemOperations**. For more details, see section [2.2.1.1.2.5](#Section_4b651a7f21fe4f2b82e71764c32b3eaa).
  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

###### Plain Text Query Value

The plain text query value uses [**plain text**](#gt_afa1b8ad-29c4-4f4a-90ce-e63b3547e15a) to specify the [**URI**](#gt_e18af8e8-01d7-4f91-8a1e-0fb21b191f95) parameters. The [**Augmented Backus-Naur Form (ABNF)**](#gt_24ddbbb4-b79e-4419-96ec-0fdd229c9ebf) notation, as specified in [\[RFC5234\]](https://go.microsoft.com/fwlink/?LinkId=123096), is used to define the syntax.

21. plain-text-query = command-spec \'&\' user-spec \'&\' device-id-spec \'&\' device-type-spec \*(\'&\' parameter-spec)

    command-spec = \"Cmd=\" command-name

    user-spec = \"User=\" user-name

    device-id-spec = \"DeviceId=\" device-id

    device-type-spec = \"DeviceType=\" device-type

    parameter-spec = parameter-name \"=\" parameter-value

    command-name = 1\*ALPHA

    user-name = 1\*VCHAR

    device-id = 1\*32(ALPHA / DIGIT)

    device-type = 1\*VCHAR

    parameter-name = 1\*ALPHA

    parameter-value = 1\*VCHAR

####### Command

The ActiveSync command to be executed is specified by the command-spec [**ABNF**](#gt_24ddbbb4-b79e-4419-96ec-0fdd229c9ebf) rule portion of the plain text query value. Valid values, represented by the command-name ABNF rule, are specified in the \"Command\" column of the table in section [2.2.1.1.1.1.2](#Section_0ab55ebc6ea94ae4af375736d5195d46).

####### User Name

The user ID of the user is specified by the user-spec [**ABNF**](#gt_24ddbbb4-b79e-4419-96ec-0fdd229c9ebf) rule portion of the plain text query value.

####### Device ID

The device ID is specified by the device-id-spec [**ABNF**](#gt_24ddbbb4-b79e-4419-96ec-0fdd229c9ebf) rule portion of the plain text query value. The value, represented by the device-id ABNF rule, is a string that specifies the device. Each device MUST have a unique device ID string. The device ID string MUST NOT contain commas or special characters. Each request from the device MUST include the same device ID string.

####### Device type

The device type is specified by the device-type-spec [**ABNF**](#gt_24ddbbb4-b79e-4419-96ec-0fdd229c9ebf) rule portion of the plain text query value. The value, represented by the device-type ABNF rule, is any string that specifies a device type. \"SP\" specifies a SmartPhone and \"PPC\" specifies a PocketPC. Other client devices send unique strings for their specific device type. Each request from a client device MUST include the same device type string.

####### Command-Specific URI Parameters

The following [**URI**](#gt_e18af8e8-01d7-4f91-8a1e-0fb21b191f95) parameters, also called command parameters, are specific to the ActiveSync commands. They are specified by the parameter-spec [**ABNF**](#gt_24ddbbb4-b79e-4419-96ec-0fdd229c9ebf) rule portion of the plain text query value. Valid values for the parameter name, represented by the parameter-name ABNF rule, are specified by the \"Parameter\" column in the following table. Valid parameter values, represented by the parameter-value ABNF rule, are specified in the \"Description\" column.

  ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Parameter        Description                                                                                                                                                                                                                                                                                                                                               Used by
  ---------------- --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- ------------------------------------------------
  AttachmentName   A string that specifies the name of the attachment file to be retrieved.                                                                                                                                                                                                                                                                                  **GetAttachment**

  CollectionId     A string that specifies the [**server ID**](#gt_3cc9da36-889e-4dc8-9c51-579d091665ae) of the folder that contains the [**Message object**](#gt_b6c15d0c-d992-421d-ba96-99d3b63894cf) to be forwarded or replied to.                                                                                                                                       **SmartForward**, **SmartReply**

  ItemId           A string that specifies the server ID of the Message object to be forwarded or replied to.                                                                                                                                                                                                                                                                **SmartForward**, **SmartReply**

  LongId           A string that references a result set that was returned in the **Search** command response.                                                                                                                                                                                                                                                               **SmartForward**, **SmartReply**

  Occurrence       A string that specifies the ID of a particular occurrence in a recurring [**meeting**](#gt_cbc56efc-e4f7-4b31-9e5f-9c44e3924d94).                                                                                                                                                                                                                         **SmartForward**, **SmartReply**

  SaveInSent       A character that specifies whether a copy of the Message object will be saved in the [**Sent Items folder**](#gt_fe856661-83ad-4264-85d4-f4c4fa4ce2cb). Set this parameter to T to instruct the server to save the Message object in the user\'s Sent Items folder; otherwise, set the parameter to F. The SaveInSent parameter is set to F by default.   **SmartForward**, **SmartReply**, **SendMail**
  ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

For more details about specific commands, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

##### Request Headers

The HTTP/1.1 protocol ([\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372)) defines several headers that can be sent from the client to the server on an **[HTTP](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) POST** request. The headers follow the request line in the HTTP portion of a request. The following headers are used in ActiveSync synchronization protocol requests. Note that requests are UTF-8 encoded.

  --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Header                 Required                                                                                       Notes
  ---------------------- ---------------------------------------------------------------------------------------------- ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Accept-Language        No.                                                                                            For details, see section [2.2.1.1.2.1](#Section_631bbe5bed55482cb0f3d3c06104fdb8).

  Authorization          Yes, if using basic or [**OAuth**](#gt_cb5ce626-9f44-4967-b6f5-cc58163db5f9) authentication.   For details, see section [2.2.1.1.2.2](#Section_7b7fabb9910c4f1c939657d7ca579a31).

  Content-Type           Depends on the command.                                                                        Specifies that the media type of the request body is WBXML. Other types of content, such as [\[RFC2822\]](https://go.microsoft.com/fwlink/?LinkId=90385), can also be specified, depending on the command. For more details, see section [2.2.1.1.2.3](#Section_f1dda4f45a0647618ae638189eb649a8).

  Cookie                 Depends on the contents of previous server responses and the protocol version in use.          Contains one or more cookies that the client previously received from the server in a Set-Cookie header. For more details, see section [2.2.1.1.2.4](#Section_f33bf269f9504d7db199cd5afc30cddb).

  MS-ASAcceptMultiPart   No                                                                                             Specifies that the client wants items returned in multipart format. For more details, see section [2.2.1.1.2.5](#Section_4b651a7f21fe4f2b82e71764c32b3eaa).

  MS-ASProtocolVersion   No if using a base64 encoded query value; yes if using a plain text query value.               Specifies the version of the ActiveSync protocol that the client supports. For more details, see section [2.2.1.1.2.6](#Section_53676b94410d48bbb92b7a88e1667dd9).

  User-Agent             No                                                                                             Contains information about the client sending the request. For more details, see section [2.2.1.1.2.7](#Section_934dd1caffe64ec3b8e43673f004c5b6).

  X-MS-PolicyKey         Depends on the command.                                                                        Specifies the policy key assigned by the server to the client. For more details, see section [2.2.1.1.2.8](#Section_f09c4f5ccb7a43438755cdab546d2d45).
  --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

###### Accept-Language

The Accept-Language header is used to define the [**locale**](#gt_7b78ebef-e35d-45ab-abfd-4121b60995de) of the client which is used when performing searches with the **Find** or **Search** command requests. If the accept language is not specified, the search is conducted by using the server language.

###### Authorization

Users authenticate through the ActiveSync protocol​ by using [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) basic authentication, [**OAuth**](#gt_cb5ce626-9f44-4967-b6f5-cc58163db5f9) or a client certificate. Credentials are passed in different formats depending upon the form of authentication.

For HTTP basic authentication, credentials are encoded with [**base64 encoding**](#gt_179b9392-9019-45a3-880b-26f6890522b7). For user *fakename* and password *x\$pIAK9@p9!*, the following is the authorization header:

36. Authorization: Basic ZmFrZXVzZXI6eCRwSUFLOUBwOSE=

For details about HTTP basic authentication, see [\[RFC1945\]](https://go.microsoft.com/fwlink/?LinkId=90300) section 11.1.

For OAuth, an access token is obtained from the authorization server in response to an authorization grant. The access token is then used to obtain a protected resource from the resource server. The following is an example of an authorization header:

37. Authorization: Bearer \<\<token\>\>

For details about the OAuth 2.0 framework, see [\[RFC6749\]](https://go.microsoft.com/fwlink/?LinkId=301486).

For authentication using a client certificate, the client MUST NOT send an authorization header. The server prompts the client for a certificate as part of the initial [**SSL/TLS handshake**](#gt_f17e8628-fadb-462b-a654-8ad363204d17) or as part of a [**TLS**](#gt_f2bc7fed-7e02-4fa5-91b3-97f5c978563a) renegotiation.

If no client certificate exists, the client MUST complete the SSL/TLS handshake.

For details about providing a client certificate during a SSL/TLS handshake, see [\[RFC5246\]](https://go.microsoft.com/fwlink/?LinkId=129803) section 7.4.6.

###### Content-Type

The Content-Type header indicates the format of the data sent in the request body. When the request body for a command is in [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc) format, the Content-Type header value MUST be set to either \"application/vnd.ms-sync.wbxml\", or the shortened string \"application/vnd.ms-sync\". The shortened string is not allowed by protocol versions 2.5 and 12.0.

For the **Autodiscover** command ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.1), which specifies an [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) request body format, the Content-Type header SHOULD be set to \"text/xml\" or MAY[\<3\>](\l) be set to \"text/html\". If the request has no body, the Content-Type header SHOULD NOT be present.

###### Cookie

The Cookie header contains one or more cookies that the client previously received from the server in a Set-Cookie header (section [2.2.2.1.2.12](#Section_f429119296d3406a9f68615d2255ae5f)). Each cookie consists of the name and value. Multiple cookies are separated by a semi-colon. For details about the syntax, see [\[RFC6265\]](https://go.microsoft.com/fwlink/?LinkId=523842).

Clients using protocol version 16.0 or 16.1 or clients using [**OAuth**](#gt_cb5ce626-9f44-4967-b6f5-cc58163db5f9) authentication for any protocol version MUST be able to parse and interpret a Set-Cookie header that is received from the server. If the server response includes the Set-Cookie header, clients using protocol version 16.0 or 16.1 or clients using OAuth authentication for any protocol version MUST provide these cookies in a Cookie header when sending future requests to the server.

###### MS-ASAcceptMultiPart

The MS-ASAcceptMultiPart header is used to control the delivery of the content requested by the **Fetch** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.67.1) in an **ItemOperations** request (\[MS-ASCMD\] section 2.2.1.10). This header is optional for **ItemOperations** requests, and SHOULD NOT be used for other command requests. This header SHOULD NOT be used if the base64-encoded query value is being used. Instead, the **AcceptMultipart** flag in the **Options** parameter SHOULD be used, as specified in section [2.2.1.1.1.1.3](#Section_49dda9263e7e4b6fa5c358630d870a47).

If this header is present and the value is \'T\', the client is requesting that the server return content in multipart format. If the header is not present, or is present and set to \'F\', the client is requesting that the server return content in inline format. For more details, see \[MS-ASCMD\] section 2.2.1.10.1.

This header is not supported by protocol version 2.5.

###### MS-ASProtocolVersion

The MS-ASProtocolVersion header indicates the protocol version that the client is using to format the request. This header SHOULD NOT be used if the base64-encoded query value is being used. Instead, the **Protocol version** field of the base64-encoded query value SHOULD be set, as specified in section [2.2.1.1.1.1](#Section_9f75a516edff48d294b95f2a3cc570d3).

The following values, which correspond to the ActiveSync protocol versions, are valid: \"16.1\", \"16.0\", \"14.1\", \"14.0\", \"12.1\", \"12.0\", and \"2.5\". The latest version is 16.1.

###### User-Agent

The format of the User-Agent header is specified in [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372) section 14.43. This header SHOULD be included in command requests.

###### X-MS-PolicyKey

The X-MS-PolicyKey header contains the client\'s current policy key, as specified in [\[MS-ASPROV\]](%5bMS-ASPROV%5d.pdf#Section_449c453b74d74919bfe895972b27048a) section 2.2.2.42. This header SHOULD NOT be used if the base64-encoded query value is being used. Instead, the **Policy key** field of the base64-encoded query value SHOULD be set, as specified in section [2.2.1.1.1.1](#Section_9f75a516edff48d294b95f2a3cc570d3).

##### Request Body

The request body contains data sent to the server. The request body, if any, is in [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc), except the **Autodiscover** command, which is in [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85). Three commands have no body in certain contexts: **GetAttachment**, **Sync**, and **Ping**. For more details about the request bodies of individual commands, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.

### HTTP POST Response

After receiving and interpreting a request, a server responds with an [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) response that contains data returned from the server.

#### Response Format

Each command response is sent from the server to the client as an [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) **POST** response. Note that these responses are UTF-8 encoded. As specified by [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372), the format is the same as for the following requests.

38. Status-line

    Response-headers

    CR/LF

    Message Body

##### Status Line

The status line consists of the [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) version and a status code. The following is an example of a response status line.

42. HTTP/1.1 200 OK

The following table lists some common HTTP status codes.

  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Status code                   Description
  ----------------------------- ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  200 OK                        The command succeeded.

  400 Bad Request               The request could not be understood by the server due to malformed syntax. If the client repeats the request without modifications, then the same error occurs.

  401 Unauthorized              The resource requires authorization or authorization was refused. For details about the client\'s handling of this error, see section [3.1.5.2.1](#Section_06ccf30465a74366b3957369da7ea1f8).

  403 Forbidden                 The user is not enabled for ActiveSync synchronization. For details about the client\'s handling of this error, see section 3.1.5.2.1.

  404 Not Found                 The specified [**URI**](#gt_e18af8e8-01d7-4f91-8a1e-0fb21b191f95) could not be found or the server is not a valid server with ActiveSync.

  451 Redirect                  The device is trying to connect to a server that cannot access the user\'s [**mailbox**](#gt_d3ad0e15-adc9-4174-bacf-d929b57278b3), or there is a more efficient server to use to reach the user\'s mailbox. For details about the client\'s handling of this error, see section [3.1.5.2.2](#Section_a907164a907c48b4b2ef44a06867f03f).

  456 Blocked                   The user\'s account is blocked. For details about the client\'s handling of this error, see section [3.1.5.2.4](#Section_a56a2b3d5bda4895be179f1e84bc2f85).

  457 Expired Password          The user\'s password has expired. For details about the client\'s handling of this error, see section 3.1.5.2.4.

  500 Internal Server Error     The server encountered an unexpected condition that prevented it from fulfilling the request. For details about the client\'s handling of this error, see section 3.1.5.2.1.

  501 Not Implemented           The server does not support the functionality that is required to fulfill the request. This status code SHOULD be returned by the server when the server does not recognize the request method or is not able to support it for any resource. In the case of other malformed requests, the server returns status code 400.

  502 Proxy Error               The specified server could not be found.

  503 Service Unavailable       The service is unavailable. For details about the client\'s handling of this error, see section [3.1.5.2.3](#Section_7eff4bdd973647a8bbdf16b8f2a754ac).

  507 Insufficient Disk Space   The user\'s mailbox is full.
  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

##### Response Headers

This protocol and [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372) define several headers that can be sent from the server to the client in an **[HTTP](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) POST** response. The headers follow the status line in the HTTP part of a response. The following table lists some common headers that can be set by the server in response to client requests.

  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Header                        Example value                                                                                                                                                                                                                                                                                      Notes
  ----------------------------- -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Cache-Control                 private                                                                                                                                                                                                                                                                                            Optional. Controls how the response is cached.

  Content-Encoding              gzip                                                                                                                                                                                                                                                                                               Required when the content is compressed; otherwise, this header is not included. Specifies the HTTP compression format that is used in the response.

  Content-Length                56                                                                                                                                                                                                                                                                                                 Optional. Specifies the size of the response body in bytes.

  Content-Type                  application/vnd.ms-sync.wbxml                                                                                                                                                                                                                                                                      Required. Specifies that the media-type of the response body is [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc). Other types of content, such as [\[RFC2822\]](https://go.microsoft.com/fwlink/?LinkId=90385), can also be specified.

  MS-Server-ActiveSync          15.1                                                                                                                                                                                                                                                                                               Optional. Indicates the version of the ActiveSync server that was used to handle the request.

  X-MS-Location                 https://mail.contoso.com/Microsoft-Server-ActiveSync                                                                                                                                                                                                                                               Optional. Used in conjunction with a 451 Redirect status code. Specifies the [**URL**](#gt_433a4fb7-ef84-46b0-ab65-905f5e3a80b1) to use for future requests.

  MS-ASProtocolCommands         Sync,SendMail,SmartForward,SmartReply,​GetAttachment,GetHierarchy,CreateCollection,​DeleteCollection,MoveCollection,FolderSync,​FolderCreate,FolderDelete,FolderUpdate,​MoveItems,GetItemEstimate,MeetingResponse,​Search,Settings,Ping,ItemOperations,​Provision,ResolveRecipients,ValidateCert, Find   Optional. Indicates the commands supported by the server.

  MS-ASProtocolVersions         2.5,12.0,12.1,14.0,14.1,16.0,16.1                                                                                                                                                                                                                                                                  Optional. Indicates the protocol versions supported by the server.

  X-MS-RP                       2.5,12.0,12.1,14.0,14.1,16.0,16.1                                                                                                                                                                                                                                                                  Optional. Indicates to the client that the client has to perform a full resynchronization.

  X-MS-Credential-Service-Url   https://portal.microsoftonline.com/ChangePassword.aspx                                                                                                                                                                                                                                             Optional. Contains a URL for reset of user\'s password.

  X-MS-Credentials-Expire       13                                                                                                                                                                                                                                                                                                 Optional. Indicates the number of days remaining until expiration of a user\'s password.

  X-MS-ASThrottle               Global                                                                                                                                                                                                                                                                                             Optional. Contains information about request being throttled.

  Set-Cookie                    X-Cookie=value; expires=Wed, 08-Jul-2015 23:40:27 GMT; path=/Microsoft-Server-ActiveSync; secure; HttpOnly                                                                                                                                                                                         Optional. Contains one or more cookies returned by the server.

  X-BEServer                    EXCH-SERV-1                                                                                                                                                                                                                                                                                        Optional. Contains the name of the server that processed the request.

  X-FEServer                    EXCH-SERV-1                                                                                                                                                                                                                                                                                        Optional. Contains the name of the server(s) that routed the request.

  request-id                    7faa449e-4912-4a79-aade-afee642c2c36                                                                                                                                                                                                                                                               Optional. Contains a server-generated identifier for the request.
  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

When protocol version 12.1, 14.0, 14.1, 16.0, or 16.1 is used: Some of the headers in the response can be eliminated when the response is to an HTTP **POST** request and the response has HTTP status 200. When these two conditions are met, only the following headers are necessary in the response:

-   Content-Length

-   Content-Type, required only if Content-Length is greater than zero.

###### Cache-Control

This header is optional. The value of this header controls how the response is cached, as specified in [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372) section 14.9.

###### Content-Encoding

This header is required if the response body is compressed. Otherwise, it is omitted. See [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372) section 14.11.

###### Content-Length

This header is optional. The format is specified in [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372) section 14.13.

###### Content-Type

This header is required. If the response body is [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc), the value of this header MUST be \"application/vnd.ms-sync.wbxml\". Otherwise, an appropriate value SHOULD be used as specified in [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372) section 14.17.

###### MS-Server-ActiveSync

This header is optional. It contains an implementation-specific string indicating the version of the server.

###### X-MS-Location

This header is optional. It is used when the [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) status code is 451 to provide a [**URL**](#gt_433a4fb7-ef84-46b0-ab65-905f5e3a80b1) to use for subsequent requests.

###### MS-ASProtocolCommands

The MS-ASProtocolCommands header contains a comma-delimited list of the ActiveSync commands supported by the server. It will be returned in an HTTP POST response if the server requires the client to reinitialize its synchronization state.

###### MS-ASProtocolVersions

The MS-ASProtocolVersions header contains a comma-delimited list of the ActiveSync protocol versions that the server supports. It will be returned in HTTP POST response headers if the server requires the client to reinitialize its synchronization state.

The following values correspond to the ActiveSync protocol versions that are specified by [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a): \"16.1\", \"16.0\", \"14.1\", \"14.0\", \"12.1\", \"12.0\", and \"2.5\". The latest version is 16.1.

###### X-MS-RP

This header is optional. Its presence in a response indicates that a condition on the server (such as a server upgrade) requires the client to discard its local data and resynchronize. The value of this header indicates the protocol versions the server supports.

This header is not supported by protocol version 2.5.

###### X-MS-Credential-Service-Url

This header is optional. This header contains the [**URL**](#gt_433a4fb7-ef84-46b0-ab65-905f5e3a80b1) for a self-service web site that allows a user to reset the user password.

This header is required in the response to an **Autodiscover** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.1) when the user\'s password is either near expiration or expired. The point at which a password is near expiration is determined by the implementer.

###### X-MS-Credentials-Expire

This header is optional. This header contains an integer that indicates the number of days remaining until the user\'s password expires. A value of 0 (zero) indicates that the password will expire in less than 24 hours.

This header can be included in a response to provide advance warning of password expiration.

###### Set-Cookie

This header is optional. The Set-Cookie header contains one cookie returned by the server. Each cookie consists of a name, a value, and the following attributes: *Expires*, *Path*, *Secure*, and *HttpOnly*. Multiple instances of this header can be returned with a different cookie name in each instance of the header.

For details about the syntax of this header, see [\[RFC6265\]](https://go.microsoft.com/fwlink/?LinkId=523842).

###### X-MS-ASThrottle

This header is optional. The X-MS-ASThrottle header specifies the condition under which the server MAY[\<4\>](\l) throttle the client device.

###### X-BEServer

This header is optional. The X-BEServer header contains the name of the server that processed the request.

###### X-FEServer

This header is optional. The X-FEServer header contains the name of the server(s) that routed the request.

###### request-id

This header is optional. The request-id header contains a server-generated identifier for the request.

##### Response Body

The response body contains data returned from the server. The response body, if any, is in [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc), except the **Autodiscover** command, which is in [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85). Two commands have no XML body in certain contexts: **GetAttachment** and **Sync**. For more details about the response bodies of individual commands, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.

### HTTP OPTIONS Request

The **[HTTP](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) OPTIONS** command, which is specified by [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372), is used to discover what protocol versions are supported, and which protocol commands are supported on the server. The client uses the HTTP **OPTIONS** command to determine whether the server supports the same versions of the protocol that the client supports.

#### Request Format

As specified by [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372), the format is as follows.

43. Request-line

    Request-headers

##### Request Line

The request line consists of the method indicator, **OPTIONS**, followed by the [**URI**](#gt_e18af8e8-01d7-4f91-8a1e-0fb21b191f95), followed by the [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) version, as follows.

45. OPTIONS \<URI\> HTTP/1.1

The URI can be either an absolute URI or a relative URI, as specified in [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372) section 3.2.1. The absolute URI consists of a scheme indicator, the host name, and the path. The relative URI consists of the path.

The path in the URI has the following format.

46. /\<ActiveSync virtual directory name\>

##### Request Headers

The authorization header is required. For more information on the authorization header requirements, see section [2.2.1.1.2.2](#Section_7b7fabb9910c4f1c939657d7ca579a31).

### HTTP OPTIONS Response

After receiving an [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) **OPTIONS** request, a server responds with an HTTP **OPTIONS** response that specifies the protocol versions it supports.

#### Response Format

Each response is sent from the server to the client as an [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) **OPTIONS** response. Note that these responses are UTF-8 encoded. As specified by [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372), the format is the same as for the following requests:

47. Status-line

    Response-headers

##### Status Line

The status line for an [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) **OPTIONS** response is identical to the status line for an HTTP **POST** response, specified in section [2.2.2.1.1](#Section_e092c31076094a3890cc10688f79cf8d).

##### Response Headers

This protocol defines headers that can be sent from the server to the client in an **[HTTP](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) OPTIONS** response in addition to headers defined in [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372). The headers follow the status line in the HTTP part of a response.

  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Header                  Example value                                                                                                                                                                                                                                                                                     Notes
  ----------------------- ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- ----------------------------------------------------------------
  MS-ASProtocolCommands   Sync,SendMail,SmartForward,SmartReply,​GetAttachment,GetHierarchy,CreateCollection,​DeleteCollection,MoveCollection,FolderSync,​FolderCreate,FolderDelete,FolderUpdate,​MoveItems,GetItemEstimate,MeetingResponse,​Search,Settings,Ping,ItemOperations,​Provision,ResolveRecipients,ValidateCert,Find   Indicates the commands supported by the server.

  MS-ASProtocolVersions   2.5,12.0,12.1,14.0,14.1,16.0,16.1                                                                                                                                                                                                                                                                 Indicates the protocol versions supported by the server.

  Set-Cookie              X-Cookie=value; expires=Wed, 08-Jul-2015 23:40:27 GMT; path=/Microsoft-Server-ActiveSync; secure; HttpOnly                                                                                                                                                                                        Optional. Contains one or more cookies returned by the server.
  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

###### MS-ASProtocolCommands

The MS-ASProtocolCommands header contains a comma-delimited list of the ActiveSync commands supported by the server.

###### MS-ASProtocolVersions

The MS-ASProtocolVersions header contains a comma-delimited list of the ActiveSync protocol versions that the server supports.

The following values correspond to the ActiveSync protocol versions that are specified by [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a): \"16.1\", \"16.0\", \"14.1\", \"14.0\", \"12.1\", \"12.0\", and \"2.5\". The latest version is 16.1.

###### Set-Cookie

For details about this header see section [2.2.2.1.2.12](#Section_f429119296d3406a9f68615d2255ae5f).

# Protocol Details

## Client Details

### Abstract Data Model

None.

### Timers

None.

### Initialization

The client SHOULD send an **Autodiscover** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.1) to the server to determine the correct server [**URL**](#gt_433a4fb7-ef84-46b0-ab65-905f5e3a80b1) to use for all subsequent commands.

After determining the correct server URL, the client SHOULD send an [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) **OPTIONS** command to the server, as specified in section [2.2.3](#Section_a8e2fea0b9c14141b9bb2f8d38fbe76c). The client SHOULD[\<5\>](\l) use the most recent version (the greatest numbered version) of the protocol that is supported by the client and server.

### Higher-Layer Triggered Events

Synchronizing changes on the client requires the client to send a command to the server.

#### Sending a Command Request

Command requests MUST be formatted as specified in section [2.2.1](#Section_0eee39a74c4d450f89ad4d9eb74bc60e) and sent via [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd). [**Secure Sockets Layer (SSL)**](#gt_d7ef66a9-f154-4d88-bda9-98bdf7235352) SHOULD be enabled between the client and the server whenever the Authorization header (section [2.2.1.1.2.2](#Section_7b7fabb9910c4f1c939657d7ca579a31)) is sent. The client SHOULD wait for a server response to the request.

Clients that include the User-Agent HTTP header SHOULD NOT change the value of this header between consecutive command requests, unless a major change to the client has occurred, such as an operating system upgrade.

### Message Processing Events and Sequencing Rules

Clients receive HTTP responses from the server only in response to HTTP requests sent by the client.

#### Handling a Successful Response

If the HTTP status code indicates that the request succeeded (its value is between 200 and 299, as specified in [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372)), the response is interpreted as specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.

If the server returns an X-MS-RP header in the response, the client MUST reinitialize its synchronization state as specified in \[MS-ASCMD\] section 2.2.1.21. If the X-MS-RP header is received in response to a **FolderSync** request that has a synchronization key of 0, the client can ignore the X-MS-RP header.

If the server returns the X-MS-Credentials-Expire header (section [2.2.2.1.2.11](#Section_4fcb4ae055d847fd9cf10cb703a6f56f)) in the response, the client SHOULD send an **Autodiscover** command request (\[MS-ASCMD\] section 2.2.1.1) to retrieve the [**URL**](#gt_433a4fb7-ef84-46b0-ab65-905f5e3a80b1) for the self-service web site that allows a user to do a password reset. The **Autodiscover** command response will include the X-MS-Credential-Service-Url header (section [2.2.2.1.2.10](#Section_1d7eef02f6714a5cb229d4ac9eb4f9d8)), which contains the URL for the self-service web site. The client SHOULD provide this URL to the user.

#### Handling a Failed Response

Any [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) status code that is not between 200 and 299, as specified in [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372), indicates that the request failed. The following sections specify the client\'s handling of certain HTTP status codes that are returned by the server when a request fails. All other HTTP status codes that indicate a failed request are interpreted and handled as specified in \[RFC2616\].

##### HTTP Error 401, 403, and 500

If the server responds to any command with an [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) error 401, 403, or 500, the client SHOULD send an **Autodiscover** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.1) to the server.

##### HTTP Error 451

If the client is attempting to connect to the wrong server (that is, a server that cannot access the user\'s mailbox), or if there is a more efficient server to use to reach the user\'s mailbox, then a 451 Redirect error is returned.

The error returned by the wrong server resembles the following:

49. OPTIONS /Microsoft-Server-ActiveSync

    Content-Type: application/vnd.ms-sync.wbxml

    MS-ASProtocolVersion: 14.0

    HTTP/1.1 451

    Date: Tue, 08 Dec 2009 19:43:24 GMT

    Server: Microsoft-IIS/7.0

    X-Powered-By: ASP.NET

    X-AspNet-Version: 2.0.50727

    X-MS-Location: https://mail.contoso.com/Microsoft-Server-ActiveSync

    Cache-Control: private

    Content-Length: 0

If an X-MS-Location header is present in the response, all subsequent requests SHOULD use the URL specified within the X-MS-Location header. If the server does not provide an X-MS-Location header in its response to the client, then the full **Autodiscover** command process is followed, as specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

##### HTTP Error 503

The server returns an HTTP error 503 when more users than are allowed by the server\'s request queue limit have sent requests to a single server or when the actions of the client have triggered throttling.

The error returned by the server resembles the following.

61. OPTIONS /Microsoft-Server-ActiveSync

    Content-Type: application/vnd.ms-sync.wbxml

    MS-ASProtocolVersion: 14.0

    HTTP/1.1 503 Service Unavailable

    Connection: close

    Date: Mon, 02 Mar 2009 23:51:51 GMT

    Server: Microsoft-IIS/7.0

    X-Powered-By: ASP.NET

    Content-Type: text/html

If a Retry-After header ([\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372)) is present in the response, the client SHOULD[\<6\>](\l) retry the request after waiting the number of seconds indicated by the Retry-After header. Any such value represents an estimate of when the server is expected to be able to process the request.

If a Retry-Header is not present in the response, the client can retry the request after waiting a few seconds. The time to wait between continuous requests that result in HTTP error 503 responses can be increased exponentially to a predetermined maximum.

For more details about ASP.NET performance monitoring properties, see [\[MSDN-APM\]](https://go.microsoft.com/fwlink/?LinkId=122592).

##### HTTP Error 456 and 457

If the server responds to an **Autodiscover** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.1) with an [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) error 456, the client SHOULD stop sending requests to the server and then prompt the user to contact the administrator.

If the server responds to an **Autodiscover** command request with an HTTP error 457, the client SHOULD stop sending requests to the server and then prompt the user to reset the user password. The client SHOULD direct the user to the self-service web site that allows a user to do a password reset. The [**URL**](#gt_433a4fb7-ef84-46b0-ab65-905f5e3a80b1) for this web site is contained in the X-MS-Credential-Service-Url header (section [2.2.2.1.2.10](#Section_1d7eef02f6714a5cb229d4ac9eb4f9d8)) of the server\'s response.

### Timer Events

None.

### Other Local Events

None.

## Server Details

The server only responds to client requests by returning HTTP responses as specified in section [2.2.2](#Section_91398f8ecaef4b2d8046af7c39014461) and never initiates communication with the client.

### Abstract Data Model

None.

### Timers

None.

### Initialization

None.

### Higher-Layer Triggered Events

None.

### Message Processing Events and Sequencing Rules

The server can receive [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) **POST** requests (section [2.2.1](#Section_0eee39a74c4d450f89ad4d9eb74bc60e)) or HTTP **OPTIONS** requests (section [2.2.3](#Section_a8e2fea0b9c14141b9bb2f8d38fbe76c)) from the client.

#### Handling HTTP POST Command Requests

The server parses HTTP **POST** requests from clients as specified in section [2.2.1](#Section_0eee39a74c4d450f89ad4d9eb74bc60e). The ActiveSync command contained within an HTTP **POST** request is parsed as specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1. The server MUST conform to the protocol version that is specified by the MS-ASProtocolVersion header (section [2.2.1.1.2.6](#Section_53676b94410d48bbb92b7a88e1667dd9)) in the client request. The server formats an HTTP **POST** response, as specified in section [2.2.2](#Section_91398f8ecaef4b2d8046af7c39014461), with an appropriate [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) status code, as specified in section [2.2.2.1.1](#Section_e092c31076094a3890cc10688f79cf8d).

If the server returns an HTTP 451 error and knows the [**URL**](#gt_433a4fb7-ef84-46b0-ab65-905f5e3a80b1) of the correct server, it SHOULD include an X-MS-Location header (section [2.2.2.1.2.6](#Section_e20706206608434d87eca98577f28be5)) with the URL of the correct server. If the server returns an HTTP 503 error, it MAY[\<7\>](\l) include a Retry-After header, as specified in [\[RFC2616\]](https://go.microsoft.com/fwlink/?LinkId=90372), in the response with an estimate of the number of seconds that will elapse before the server is expected to be able to process the request. If the server returns an HTTP 457 error, it MUST include the X-MS-Credential-Service-Url header (section [2.2.2.1.2.10](#Section_1d7eef02f6714a5cb229d4ac9eb4f9d8)) in its response.

If the user\'s password is near expiration, the server SHOULD include the X-MS-Credentials-Expire header (section [2.2.2.1.2.11](#Section_4fcb4ae055d847fd9cf10cb703a6f56f)) in its response. This header provides advance warning to the client of password expiration. The point at which the server begins this warning is determined by the implementer.

If the client sends a request to synchronize the folder hierarchy with a synchronization key of 0 or the server requires the client to reinitialize its synchronization state, the server SHOULD include an X-MS-RP header, an MS-ASProtocolCommands header, and an MS-ASProtocolVersions header in its response to the client.

The server MAY[\<8\>](\l) track the value of the User-Agent header for consecutive command requests from a specific device and block devices that change the value of this header more than a configured limit within a configured timespan. Servers that do this tracking SHOULD use the algorithm specified in section [3.2.5.1.1](#Section_a6d6e8a9b2d84220b7278c2529b0ecbb).

##### User-Agent Change Tracking

Servers SHOULD limit changes to the User-Agent header value from a device to two changes within a 24-hour time period, but MAY[\<9\>](\l) use different values for the number of changes or the time period. The server SHOULD block clients that exceed this limit for 14 hours, but MAY[\<10\>](\l) block clients for a different amount of time.

#### Handling HTTP OPTIONS Command Requests

The server parses HTTP **OPTIONS** requests from clients as specified in section [2.2.3](#Section_a8e2fea0b9c14141b9bb2f8d38fbe76c) and formats its response as specified in section [2.2.4](#Section_5eb171a0eadc46ddbef81e46d4e5ffd1). The server\'s response MUST contain both the MS-ASProtocolCommands header, as specified in section [2.2.4.1.2.1](#Section_539f72e850854840b7f22a41f89358be), and the MS-ASProtocolVersions header, as specified in section [2.2.4.1.2.2](#Section_6ba4775d24c94132b30e1f86b1b3b868). The server uses these headers to indicate which ActiveSync commands and which ActiveSync protocol versions it supports.

A protocol server can support multiple versions of the ActiveSync protocol. This specification, and any protocol specifications that cite it as a dependency, apply to the server configuration when the value of the MS-ASProtocolVersions header is set to a value that includes \"16.1\", \"16.0\", \"14.1\", \"14.0\", \"12.1\", \"12.0\", or \"2.5\".\<11\>

The latest version of the ActiveSync protocol is 16.1. Older versions are 16.0, 14.1, 14.0, 12.1, 12.0, and 2.5. Some commands and functionality described in the ActiveSync protocol documentation are not supported by all of the protocol versions. See the command and element descriptions in the ActiveSync protocol documents to determine which commands, elements, and capabilities are supported by the protocol versions.

### Timer Events

None.

### Other Local Events

None.

# Protocol Examples

## FolderSync Request and Response

The following is a typical ActiveSync protocol command request. The **FolderSync** command, user [**alias**](#gt_d046b6e2-3f79-47e1-87d7-754566744dcd), device ID, and device type are specified as [**URI**](#gt_e18af8e8-01d7-4f91-8a1e-0fb21b191f95) query parameters. The Content-Type header specifies that the request body is WBXML. The MS-ASProtocolVersion header specifies that protocol 14.0 is being used. Some command requests contain additional URI query parameters or do not specify a request body. The **HTTP POST URI** command parameter is the same as the command in the topmost element of the request XML body. For details about the commands and associated [**XML schema definitions (XSDs)**](#gt_c7e91c99-e45a-44c2-a08a-c34f137a2cae), see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a). The WBXML-encoded body is decoded for clarity.

Request

71. POST /Microsoft-Server-ActiveSync?Cmd=FolderSync&User=fakename&DeviceId=v140Device&DeviceType=SmartPhone HTTP/1.1

    Content-Type: application/vnd.ms-sync.wbxml

    MS-ASProtocolVersion: 14.0

    User-Agent: ASOM

    Host: Contoso.com

    \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

    \<FolderSync xmlns=\"FolderHierarchy:\"\>

    \<SyncKey\>2\</SyncKey\>

    \</FolderSync\>

The following is a typical FolderSync command response. The status line specifies the HTTP/1.1 protocol and that the command succeeded. The Content-Length header specifies that the response body is 56 bytes and the Content-Type header shows that the response body is in WBXML format. Some command responses do not contain WBXML bodies.

Response

81. HTTP/1.1 200 OK

    Content-Type: application/vnd.ms-sync.wbxml

    Date: Thu, 12 Mar 2009 19:34:31 GMT

    Content-Length: 25

    \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

    \<FolderSync\>

    \<Status\>1\</Status\>

    \<SyncKey\>2\</SyncKey\>

    \<Changes\>

    \<Count\>0\</Count\>

    \</Changes\>

    \</FolderSync\>

## FolderSync Request and Redirect Response

The following is the same request from the example described in section [4.1](#Section_87f79cfc4ec54dffb83853f0496df376). In this example, configuration changes on the server have caused the \"contoso.com\" host to no longer be the optimal host for the user.

Request

96. POST /Microsoft-Server-ActiveSync?Cmd=FolderSync&User=fakename&DeviceId=v140Device&DeviceType=SmartPhone HTTP/1.1

    Content-Type: application/vnd.ms-sync.wbxml

    MS-ASProtocolVersion: 14.0

    User-Agent: ASOM

    Host: Contoso.com

    \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

    \<FolderSync xmlns=\"FolderHierarchy:\"\>

    \<SyncKey\>2\</SyncKey\>

    \</FolderSync\>

The server redirects the client to the \"mail.contoso.com\" host using an [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) status code 451 and the **X-MS-Location** header.

106. HTTP/1.1 451

     Date: Thu, 12 Mar 2009 20:16:22 GMT

     X-MS-Location: https://mail.contoso.com/Microsoft-Server-ActiveSync

     Content-Length: 0

## HTTP OPTIONS Command Request and Response

The following example illustrates the use of the **HTTP OPTIONS** command. The MS-ASProtocolVersions header in the server response shows that versions 1.0, 2.0, 2.1, 2.5, 12.0, 12.1, and 14.0 of the protocol are supported on the server. The MS-ASProtocolCommands header in the server response lists the commands that are supported. It is recommended that protocol clients not trigger on the build number of the protocol server, which can change because of server updates. The build number shown in the examples might differ from those seen in a development or production environment.

Request

110. OPTIONS /Microsoft-Server-ActiveSync HTTP/1.1

     Host: Contoso.com

Response

112. HTTP/1.1 200 OK

     Cache-Control: private

     Allow: OPTIONS,POST

     Server: Microsoft-IIS/7.0

     MS-Server-ActiveSync: 14.00.0536.000

     MS-ASProtocolVersions: 2.0,2.1,2.5,12.0,12.1,14.0

     MS-ASProtocolCommands: Sync,SendMail,SmartForward,SmartReply,GetAttachment,GetHierarchy,

     CreateCollection,DeleteCollection,MoveCollection,FolderSync,FolderCreate,

     FolderDelete,FolderUpdate,MoveItems,GetItemEstimate,MeetingResponse,Search,

     Settings,Ping,ItemOperations,Provision,ResolveRecipients,ValidateCert

     Public: OPTIONS,POST

     X-AspNet-Version: 2.0.50727

     X-Powered-By: ASP.NET

     Date: Thu, 12 Mar 2009 20:03:29 GMT

     Content-Length: 0

## SendMail Request and Response

The following example illustrates the command to send mail to a specific user.

Request

128. POST /Microsoft-Server-ActiveSync?Cmd=SendMail&User=fakeusername&DeviceId=v140Device&DeviceType=SmartPhone HTTP/1.1

     Content-Type: application/vnd.ms-sync.wbxml

     MS-ASProtocolVersion: 14.0

     X-MS-PolicyKey: 2034202722

     User-Agent: ASOM

     Host: BIRSKK-dom.extest.microsoft.com

     \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<SendMail

     xmlns=\"ComposeMail:\"\>

     \<ClientId\>633724606026842453\</ClientId\>

     \<Mime\>From: fakeuser@Contoso.com

     To: fakeuser@Contoso.com

     Cc:

     Bcc:

     Subject: From NSync

     MIME-Version: 1.0

     Content-Type: text/plain; charset=\"iso-8859-1\"

     Content-Transfer-Encoding: 7bit

     X-MimeOLE: Produced By Microsoft MimeOLE V6.00.2900.3350

     This is the body text.\</Mime\>

     \</SendMail\>

Response

150. HTTP/1.1 200 OK

     s

     Date: Thu, 12 Mar 2009 20:16:22 GMT

     Content-Length: 0

## CreateFolder Request and Response

The following example illustrates the command to create a new folder. For details about the associated XML schema definitions (XSD), see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

Request:

155. POST /Microsoft-Server-ActiveSync?Cmd=FolderCreate&User=fakename@Contoso.com&DeviceId=v140Device&DeviceType=SmartPhone HTTP/1.1

     Content-Type: application/vnd.ms-sync.wbxml

     MS-ASProtocolVersion: 14.0

     User-Agent: ASOM

     Host: Contoso.com

     \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<FolderCreate xmlns=\"FolderHierarchy:\"\>

     \<SyncKey\>3\</SyncKey\>

     \<ParentId\>5\</ParentId\>

     \<DisplayName\>CreateNewFolder\</DisplayName\>

     \<Type\>12\</Type\>

     \</FolderCreate\>

Response:

168. HTTP/1.1 200 OK

     Content-Type: application/vnd.ms-sync.wbxml

     Date: Thu, 12 Mar 2009 20:s26:06 GMT

     Content-Length: 24

     \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<FolderCreate

     xmlns=\"FolderHierarchy:\"\>

     \<Status\>1\</Status\>

     \<SyncKey\>4\</SyncKey\>

     \<ServerId\>23\</ServerId\>

     \</FolderCreate\>

# Security

## Security Considerations for Implementers

There are no special security considerations specific to this specification. It is recommended that communication between the client and server occur across an HTTP connection secured by the [**Secure Sockets Layer (SSL)**](#gt_d7ef66a9-f154-4d88-bda9-98bdf7235352) protocol.

When connecting to a server using SSL, clients are required to support server certificates that use the Subject Alternative Name for domain names, as specified in [\[RFC4985\]](https://go.microsoft.com/fwlink/?LinkId=193320), as well as wildcard certificate names, as specified in [\[RFC2818\]](https://go.microsoft.com/fwlink/?LinkId=90383) and [\[RFC3280\]](https://go.microsoft.com/fwlink/?LinkId=90414).

## Index of Security Parameters

None.

# Appendix A: Product Behavior

The information in this specification is applicable to the following Microsoft products or supplemental software. References to product versions include updates to those products.

-   Microsoft Exchange Server 2007 Service Pack 1 (SP1)

-   Microsoft Exchange Server 2010

-   Microsoft Exchange Server 2013

-   Microsoft Exchange Server 2016

-   Microsoft Exchange Server 2019

-   Windows 8.1

-   Windows 10 operating system

-   Windows Server 2016 operating system

-   Windows 11 operating system

-   Microsoft Exchange Server Subscription Edition

Exceptions, if any, are noted in this section. If an update version, service pack or Knowledge Base (KB) number appears with a product name, the behavior changed in that update. The new behavior also applies to subsequent updates unless otherwise specified. If a product edition appears with the product version, behavior is different in that product edition.

Unless otherwise specified, any statement of optional behavior in this specification that is prescribed using the terms \"SHOULD\" or \"SHOULD NOT\" implies product behavior in accordance with the SHOULD or SHOULD NOT prescription. Unless otherwise specified, the term \"MAY\" implies that the product does not follow the prescription.

\<1\> Section 2.2.1.1.1.1: Exchange 2007 SP1 and the initial release version of Exchange 2010 do not set the **Protocol version** field to 141, 160 or 161. Microsoft Exchange Server 2010 Service Pack 1 (SP1) and Exchange 2013 do not set the **Protocol version** field to 160 or 161.

[\<2\> Section 2.2.1.1.1.1](\l): Exchange 2007 SP1 sets the **Protocol version** field to 121. The initial release version of Exchange 2010 sets the **Protocol version** field to 140.

[\<3\> Section 2.2.1.1.2.3](\l): Exchange 2007 SP1 accepts a Content-Type header value of either \"text/xml\" or \"text/html\" for the **Autodiscover** command.

[\<4\> Section 2.2.2.1.2.13](\l): The X-MS-ASThrottle header and throttling are not supported in Exchange 2007 SP1 and Exchange 2010. The X-MS-ASThrottle header and throttling are supported in Exchange 2013, Exchange 2016, and Exchange 2019 but are disabled by default.

\<5\> Section 3.1.3: Windows Communication Apps only support protocol versions 12.1 and 14.0.

[\<6\> Section 3.1.5.2.3](\l): Windows Communication Apps ignore the Retry-After header and instead retry after a set time. The set time increases exponentially when multiple 503 errors are received.

[\<7\> Section 3.2.5.1](\l): Exchange 2010 and Exchange 2013 include a Retry-After header with HTTP 503 error responses.

[\<8\> Section 3.2.5.1](\l): Exchange 2013, Exchange 2016, and Exchange 2019 can be configured to track changes to the User-Agent header, but do not do so by default.

[\<9\> Section 3.2.5.1.1](\l): Exchange 2013, Exchange 2016, and Exchange 2019 can be configured to use different values for the allowed number of changes and the time period.

[\<10\> Section 3.2.5.1.1](\l): Exchange 2013, Exchange 2016, and Exchange 2019 can be configured to block clients for an amount of time other than 14 hours.

[\<11\> Section 3.2.5.2](\l): Exchange 2007 SP1 does not return the value \"16.1\", \"16.0\", \"14.1\", or \"14.0\" in the MS-ASProtocolVersions header. The initial release version of Exchange 2010 does not return the value \"16.1\", \"16.0\" or \"14.1\" in the MS-ASProtocolVersions header. Exchange 2010 SP1 and Exchange 2013 do not return the value \"16.1\" or \"16.0\" in the MS-ASProtocolVersions header.

# Change Tracking

This section identifies changes that were made to this document since the last release. Changes are classified as Major, Minor, or None.

The revision class **Major** means that the technical content in the document was significantly revised. Major changes affect protocol interoperability or implementation. Examples of major changes are:

-   A document revision that incorporates changes to interoperability requirements.

-   A document revision that captures changes to protocol functionality.

The revision class **Minor** means that the meaning of the technical content was clarified. Minor changes do not affect protocol interoperability or implementation. Examples of minor changes are updates to clarify ambiguity at the sentence, paragraph, or table level.

The revision class **None** means that no new technical changes were introduced. Minor editorial and formatting changes may have been made, but the relevant technical content is identical to the last released version.

The changes made to this document are listed in the following table. For more information, please contact <dochelp@microsoft.com>.

  ------------------------------------------------------------------------------------------------------------------------------------
  Section                                                                       Description                           Revision class
  ----------------------------------------------------------------------------- ------------------------------------- ----------------
  [6](#Section_13d205d307cf41d889cf74acb9d191e5) Appendix A: Product Behavior   Updated list of supported products.   Major

  ------------------------------------------------------------------------------------------------------------------------------------

# Index

A

Abstract data model

[client](#abstract-data-model) 27

[server](#abstract-data-model-1) 29

[Applicability](#applicability-statement) 11

C

[Capability negotiation](#versioning-and-capability-negotiation) 11

[Change tracking](#change-tracking) 39

Client

[abstract data model](#abstract-data-model) 27

[higher-layer triggered events](#higher-layer-triggered-events) 27

[initialization](#initialization) 27

[message processing](#message-processing-events-and-sequencing-rules) 27

[other local events](#other-local-events) 29

[sequencing rules](#message-processing-events-and-sequencing-rules) 27

[timer events](#timer-events) 29

[timers](#timers) 27

[CreateFolder example](#createfolder-request-and-response) 34

D

Data model - abstract

[client](#abstract-data-model) 27

[server](#abstract-data-model-1) 29

E

Examples

[CreateFolder](#createfolder-request-and-response) 34

[FolderSync](#foldersync-request-and-response) 32

[FolderSync redirect response](#foldersync-request-and-redirect-response) 32

[HTTP OPTIONS](#http-options-command-request-and-response) 33

[SendMail](#sendmail-request-and-response) 33

F

[Fields - vendor-extensible](#vendor-extensible-fields) 11

[FolderSync example](#foldersync-request-and-response) 32

[FolderSync redirect response example](#foldersync-request-and-redirect-response) 32

G

[Glossary](#glossary) 7

H

Higher-layer triggered events

[client](#higher-layer-triggered-events) 27

[server](#higher-layer-triggered-events-1) 29

[HTTP OPTIONS example](#http-options-command-request-and-response) 33

[HTTP OPTIONS Request message](#http-options-request) 25

[HTTP OPTIONS Response message](#http-options-response) 25

[HTTP POST Request message](#http-post-request) 12

[HTTP POST Response message](#http-post-response) 20

I

[Implementer - security considerations](#security-considerations-for-implementers) 36

[Index of security parameters](#index-of-security-parameters) 36

[Informative references](#informative-references) 10

Initialization

[client](#initialization) 27

[server](#initialization-1) 29

[Introduction](#introduction) 7

M

Message processing

[client](#message-processing-events-and-sequencing-rules) 27

[server](#message-processing-events-and-sequencing-rules-1) 29

Messages

[HTTP OPTIONS Request](#http-options-request) 25

[HTTP OPTIONS Response](#http-options-response) 25

[HTTP POST Request](#http-post-request) 12

[HTTP POST Response](#http-post-response) 20

[transport](#transport) 12

N

[Normative references](#normative-references) 9

O

Other local events

[client](#other-local-events) 29

[server](#other-local-events-1) 31

[Overview (synopsis)](#overview) 10

P

[Parameters - security index](#index-of-security-parameters) 36

[Preconditions](#prerequisitespreconditions) 10

[Prerequisites](#prerequisitespreconditions) 10

[Product behavior](#appendix-a-product-behavior) 37

R

[References](#references) 9

[informative](#informative-references) 10

[normative](#normative-references) 9

[Relationship to other protocols](#relationship-to-other-protocols) 10

S

Security

[implementer considerations](#security-considerations-for-implementers) 36

[parameter index](#index-of-security-parameters) 36

[SendMail example](#sendmail-request-and-response) 33

Sequencing rules

[client](#message-processing-events-and-sequencing-rules) 27

[server](#message-processing-events-and-sequencing-rules-1) 29

Server

[abstract data model](#abstract-data-model-1) 29

[higher-layer triggered events](#higher-layer-triggered-events-1) 29

[initialization](#initialization-1) 29

[message processing](#message-processing-events-and-sequencing-rules-1) 29

[other local events](#other-local-events-1) 31

[overview](#server-details) 29

[sequencing rules](#message-processing-events-and-sequencing-rules-1) 29

[timer events](#timer-events-1) 31

[timers](#timers-1) 29

[Standards assignments](#standards-assignments) 11

T

Timer events

[client](#timer-events) 29

[server](#timer-events-1) 31

Timers

[client](#timers) 27

[server](#timers-1) 29

[Tracking changes](#change-tracking) 39

[Transport](#transport) 12

Triggered events - higher-layer

[client](#higher-layer-triggered-events) 27

[server](#higher-layer-triggered-events-1) 29

V

[Vendor-extensible fields](#vendor-extensible-fields) 11

[Versioning](#versioning-and-capability-negotiation) 11
