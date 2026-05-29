**\[MS-ASWBXML\]:**

**Exchange ActiveSync: WAP Binary XML (WBXML) Algorithm**

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
  12/3/2008    1.0.0              Major            Initial Release.

  4/10/2009    2.0.0              Major            Updated technical content and applicable product releases.

  7/15/2009    3.0.0              Major            Revised and edited for technical content.

  11/4/2009    4.0.0              Major            Updated and revised the technical content.

  2/10/2010    5.0.0              Major            Updated and revised the technical content.

  5/5/2010     6.0.0              Major            Updated and revised the technical content.

  8/4/2010     7.0                Major            Significantly changed the technical content.

  11/3/2010    8.0                Major            Significantly changed the technical content.

  3/18/2011    8.1                Minor            Clarified the meaning of the technical content.

  8/5/2011     9.0                Major            Significantly changed the technical content.

  10/7/2011    10.0               Major            Significantly changed the technical content.

  1/20/2012    11.0               Major            Significantly changed the technical content.

  4/27/2012    11.1               Minor            Clarified the meaning of the technical content.

  7/16/2012    12.0               Major            Significantly changed the technical content.

  10/8/2012    12.1               Minor            Clarified the meaning of the technical content.

  2/11/2013    12.1               None             No changes to the meaning, language, or formatting of the technical content.

  7/26/2013    13.0               Major            Significantly changed the technical content.

  11/18/2013   13.0               None             No changes to the meaning, language, or formatting of the technical content.

  2/10/2014    13.0               None             No changes to the meaning, language, or formatting of the technical content.

  4/30/2014    14.0               Major            Significantly changed the technical content.

  7/31/2014    14.0               None             No changes to the meaning, language, or formatting of the technical content.

  10/30/2014   14.1               Minor            Clarified the meaning of the technical content.

  5/26/2015    15.0               Major            Significantly changed the technical content.

  6/30/2015    16.0               Major            Significantly changed the technical content.

  9/14/2015    17.0               Major            Significantly changed the technical content.

  6/9/2016     18.0               Major            Significantly changed the technical content.

  2/28/2017    19.0               Major            Significantly changed the technical content.

  6/20/2017    20.0               Major            Significantly changed the technical content.

  7/24/2018    21.0               Major            Significantly changed the technical content.

  10/1/2018    22.0               Major            Significantly changed the technical content.

  12/11/2018   22.1               Minor            Clarified the meaning of the technical content.

  3/19/2019    22.2               Minor            Clarified the meaning of the technical content.

  4/29/2022    23.0               Major            Significantly changed the technical content.

  5/20/2025    24.0               Major            Significantly changed the technical content.
  -------------------------------------------------------------------------------------------------------------------------------

Table of Contents

[1 Introduction [5](#introduction)](#introduction)

[1.1 Glossary [5](#glossary)](#glossary)

[1.2 References [5](#references)](#references)

[1.2.1 Normative References [6](#normative-references)](#normative-references)

[1.2.2 Informative References [6](#informative-references)](#informative-references)

[1.3 Overview [6](#overview)](#overview)

[1.4 Relationship to Protocols and Other Algorithms [6](#relationship-to-protocols-and-other-algorithms)](#relationship-to-protocols-and-other-algorithms)

[1.5 Applicability Statement [7](#applicability-statement)](#applicability-statement)

[1.6 Standards Assignments [8](#standards-assignments)](#standards-assignments)

[2 Algorithm Details [9](#algorithm-details)](#algorithm-details)

[2.1 ActiveSync WBXML Algorithm Details [9](#activesync-wbxml-algorithm-details)](#activesync-wbxml-algorithm-details)

[2.1.1 Abstract Data Model [9](#abstract-data-model)](#abstract-data-model)

[2.1.2 Initialization [9](#initialization)](#initialization)

[2.1.2.1 Code Pages [9](#code-pages)](#code-pages)

[2.1.2.1.1 Code Page 0: AirSync [10](#code-page-0-airsync)](#code-page-0-airsync)

[2.1.2.1.2 Code Page 1: Contacts [11](#code-page-1-contacts)](#code-page-1-contacts)

[2.1.2.1.3 Code Page 2: Email [13](#code-page-2-email)](#code-page-2-email)

[2.1.2.1.4 Code Page 3: AirNotify [16](#code-page-3-airnotify)](#code-page-3-airnotify)

[2.1.2.1.5 Code Page 4: Calendar [16](#code-page-4-calendar)](#code-page-4-calendar)

[2.1.2.1.6 Code Page 5: Move [17](#code-page-5-move)](#code-page-5-move)

[2.1.2.1.7 Code Page 6: GetItemEstimate [18](#code-page-6-getitemestimate)](#code-page-6-getitemestimate)

[2.1.2.1.8 Code Page 7: FolderHierarchy [18](#code-page-7-folderhierarchy)](#code-page-7-folderhierarchy)

[2.1.2.1.9 Code Page 8: MeetingResponse [19](#code-page-8-meetingresponse)](#code-page-8-meetingresponse)

[2.1.2.1.10 Code Page 9: Tasks [20](#code-page-9-tasks)](#code-page-9-tasks)

[2.1.2.1.11 Code Page 10: ResolveRecipients [21](#code-page-10-resolverecipients)](#code-page-10-resolverecipients)

[2.1.2.1.12 Code Page 11: ValidateCert [22](#code-page-11-validatecert)](#code-page-11-validatecert)

[2.1.2.1.13 Code Page 12: Contacts2 [22](#code-page-12-contacts2)](#code-page-12-contacts2)

[2.1.2.1.14 Code Page 13: Ping [23](#code-page-13-ping)](#code-page-13-ping)

[2.1.2.1.15 Code Page 14: Provision [23](#code-page-14-provision)](#code-page-14-provision)

[2.1.2.1.16 Code Page 15: Search [25](#code-page-15-search)](#code-page-15-search)

[2.1.2.1.17 Code Page 16: GAL [27](#code-page-16-gal)](#code-page-16-gal)

[2.1.2.1.18 Code Page 17: AirSyncBase [27](#code-page-17-airsyncbase)](#code-page-17-airsyncbase)

[2.1.2.1.19 Code Page 18: Settings [29](#code-page-18-settings)](#code-page-18-settings)

[2.1.2.1.20 Code Page 19: DocumentLibrary [30](#code-page-19-documentlibrary)](#code-page-19-documentlibrary)

[2.1.2.1.21 Code Page 20: ItemOperations [31](#code-page-20-itemoperations)](#code-page-20-itemoperations)

[2.1.2.1.22 Code Page 21: ComposeMail [32](#code-page-21-composemail)](#code-page-21-composemail)

[2.1.2.1.23 Code Page 22: Email2 [33](#code-page-22-email2)](#code-page-22-email2)

[2.1.2.1.24 Code Page 23: Notes [34](#code-page-23-notes)](#code-page-23-notes)

[2.1.2.1.25 Code Page 24: RightsManagement [34](#code-page-24-rightsmanagement)](#code-page-24-rightsmanagement)

[2.1.2.1.26 Code Page 25: Find [35](#code-page-25-find)](#code-page-25-find)

[2.1.3 Processing Rules [36](#processing-rules)](#processing-rules)

[3 Algorithm Examples [37](#algorithm-examples)](#algorithm-examples)

[4 Security [40](#security)](#security)

[4.1 Security Considerations for Implementers [40](#security-considerations-for-implementers)](#security-considerations-for-implementers)

[4.2 Index of Security Parameters [40](#index-of-security-parameters)](#index-of-security-parameters)

[5 Appendix A: Product Behavior [41](#appendix-a-product-behavior)](#appendix-a-product-behavior)

[6 Change Tracking [42](#change-tracking)](#change-tracking)

[7 Index [43](#index)](#index)

# Introduction

The Exchange ActiveSync: WAP Binary XML (WBXML) Algorithm incorporates [**Wireless Application Protocol (WAP) Binary XML (WBXML)**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc) functionality into the ActiveSync protocols by mapping ActiveSync [**XML schema**](#gt_bd0ce6f9-c350-4900-827e-951265294067) elements to numeric code page values. This enables ActiveSync clients to encode [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) requests into WBXML for transmission to an ActiveSync server.

Sections 1.6 and 2 of this specification are normative. All other sections and examples in this specification are informative.

## Glossary

This document uses the following terms:

> []{#gt_48d3e923-3081-4b1c-a8b4-db07cc022128 .anchor}**contact**: A person, company, or other entity that is stored in a directory and is associated with one or more unique identifiers and attributes, such as an Internet message address or login name.
>
> []{#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd .anchor}**Hypertext Transfer Protocol (HTTP)**: An application-level protocol for distributed, collaborative, hypermedia information systems (text, graphic images, sound, video, and other multimedia files) on the World Wide Web.
>
> []{#gt_d7ef66a9-f154-4d88-bda9-98bdf7235352 .anchor}**Secure Sockets Layer (SSL)**: A security protocol that supports confidentiality and integrity of messages in client and server applications that communicate over open networks. SSL supports server and, optionally, client authentication using X.509 certificates [\[X509\]](https://go.microsoft.com/fwlink/?LinkId=90590) and [\[RFC5280\]](https://go.microsoft.com/fwlink/?LinkId=131034). SSL is superseded by Transport Layer Security (TLS). TLS version 1.0 is based on SSL version 3.0 [\[SSL3\]](https://go.microsoft.com/fwlink/?LinkId=90534).
>
> []{#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1 .anchor}**WBXML code page**: A map used to convert XML to Wireless Application Protocol (WAP) Binary XML (WBXML) that specifies the one-to-one relationships between the XML tags of a namespace and the tags\' numeric representations called WBXML tokens. Each WBXML code page corresponds to an XML namespace.
>
> []{#gt_ec12fbbc-e750-435b-bc6e-919038ba9127 .anchor}**WBXML token**: A single-byte code that represents a specific XML tag.
>
> []{#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc .anchor}**Wireless Application Protocol (WAP) Binary XML (WBXML)**: A compact binary representation of [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) that is designed to reduce the transmission size of XML documents over narrowband communication channels.
>
> []{#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85 .anchor}**XML**: The Extensible Markup Language, as described in [\[XML1.0\]](https://go.microsoft.com/fwlink/?LinkId=90599).
>
> []{#gt_485f05b3-df3b-45ac-b8bf-d05f5d185a24 .anchor}**XML namespace**: A collection of names that is used to identify elements, types, and attributes in XML documents identified in a URI reference [\[RFC3986\]](https://go.microsoft.com/fwlink/?LinkId=90453). A combination of XML namespace and local name allows XML documents to use elements, types, and attributes that have the same names but come from different sources. For more information, see [\[XMLNS-2ED\]](https://go.microsoft.com/fwlink/?LinkId=90602).
>
> []{#gt_bd0ce6f9-c350-4900-827e-951265294067 .anchor}**XML schema**: A description of a type of XML document that is typically expressed in terms of constraints on the structure and content of documents of that type, in addition to the basic syntax constraints that are imposed by [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) itself. An XML schema provides a view of a document type at a relatively high level of abstraction.
>
> **MAY, SHOULD, MUST, SHOULD NOT, MUST NOT:** These terms (in all caps) are used as defined in [\[RFC2119\]](https://go.microsoft.com/fwlink/?LinkId=90317). All statements of optional behavior use either MAY, SHOULD, or SHOULD NOT.

## References

Links to a document in the Microsoft Open Specifications library point to the correct section in the most recently published version of the referenced document. However, because individual documents in the library are not updated at the same time, the section numbers in the documents may not match. You can confirm the correct section numbering by checking the [Errata](https://go.microsoft.com/fwlink/?linkid=850906).

### Normative References

We conduct frequent surveys of the normative references to assure their continued availability. If you have any issue with finding a normative reference, please contact <dochelp@microsoft.com>. We will assist you in finding the relevant information.

\[MS-ASCMD\] Microsoft Corporation, \"[Exchange ActiveSync: Command Reference Protocol](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a)\".

\[MS-ASHTTP\] Microsoft Corporation, \"[Exchange ActiveSync: HTTP Protocol](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d)\".

\[RFC2119\] Bradner, S., \"Key words for use in RFCs to Indicate Requirement Levels\", BCP 14, RFC 2119, March 1997, [https://www.rfc-editor.org/info/rfc2119](https://go.microsoft.com/fwlink/?LinkId=90317)

\[WBXML1.2\] Martin, B., and Jano, B., Eds., \"WAP Binary XML Content Format\", W3C Note, June 1999, [http://www.w3.org/1999/06/NOTE-wbxml-19990624](https://go.microsoft.com/fwlink/?LinkId=160492)

### Informative References

\[MS-ASAIRS\] Microsoft Corporation, \"[Exchange ActiveSync: AirSyncBase Namespace Protocol](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c)\".

\[MS-ASCAL\] Microsoft Corporation, \"[Exchange ActiveSync: Calendar Class Protocol](%5bMS-ASCAL%5d.pdf#Section_0c4486824a6a459aae662fed0712bef9)\".

\[MS-ASCNTC\] Microsoft Corporation, \"[Exchange ActiveSync: Contact Class Protocol](%5bMS-ASCNTC%5d.pdf#Section_a4593b9dd9af4d27bc5c67c4c1b98d54)\".

\[MS-ASDOC\] Microsoft Corporation, \"[Exchange ActiveSync: Document Class Protocol](%5bMS-ASDOC%5d.pdf#Section_c503701c0e594beb9b8b038cd69a3443)\".

\[MS-ASEMAIL\] Microsoft Corporation, \"[Exchange ActiveSync: Email Class Protocol](%5bMS-ASEMAIL%5d.pdf#Section_f3d27369e0f54164aa5e9b1abda16f5f)\".

\[MS-ASNOTE\] Microsoft Corporation, \"[Exchange ActiveSync: Notes Class Protocol](%5bMS-ASNOTE%5d.pdf#Section_21801d6c000e413c859150430a8e9fd9)\".

\[MS-ASPROV\] Microsoft Corporation, \"[Exchange ActiveSync: Provisioning Protocol](%5bMS-ASPROV%5d.pdf#Section_449c453b74d74919bfe895972b27048a)\".

\[MS-ASRM\] Microsoft Corporation, \"[Exchange ActiveSync: Rights Management Protocol](%5bMS-ASRM%5d.pdf#Section_71e681b7e1784c1096b678df7fa77dfc)\".

\[MS-ASTASK\] Microsoft Corporation, \"[Exchange ActiveSync: Tasks Class Protocol](%5bMS-ASTASK%5d.pdf#Section_b8fe266450ba4d00bf6be4deab352c89)\".

\[MS-OXPROTO\] Microsoft Corporation, \"[Exchange Server Protocols System Overview](%5bMS-OXPROTO%5d.pdf#Section_734ab967e43e425babe1974af56c0283)\".

## Overview

This algorithm maps ActiveSync [**XML schema**](#gt_bd0ce6f9-c350-4900-827e-951265294067) elements to numeric code page values for use by the ActiveSync protocols, enabling ActiveSync clients to encode [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc) requests for transmission to an ActiveSync server.

This algorithm provides [**WBXML tokens**](#gt_ec12fbbc-e750-435b-bc6e-919038ba9127) and [**WBXML code pages**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) that allow clients and servers to compress the [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) tags in request and response messages. WBXML code pages are hard-coded on the server and cannot be changed by the client. The WBXML code pages correspond to [**XML namespaces**](#gt_485f05b3-df3b-45ac-b8bf-d05f5d185a24).

## Relationship to Protocols and Other Algorithms

The [**XML namespaces**](#gt_485f05b3-df3b-45ac-b8bf-d05f5d185a24) that are used by this algorithm are described in the ActiveSync protocol specifications, as shown in the following table.

The [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) numbers in the table are listed in decimal format.

  --------------------------------------------------------------------------------------------------------------------------------------
  WBXML code page                         Specification
  --------------------------------------- ----------------------------------------------------------------------------------------------
  WBXML code page 0: AirSync              [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21

  WBXML code page 1: Contacts             [\[MS-ASCNTC\]](%5bMS-ASCNTC%5d.pdf#Section_a4593b9dd9af4d27bc5c67c4c1b98d54)

  WBXML code page 2: Email                [\[MS-ASEMAIL\]](%5bMS-ASEMAIL%5d.pdf#Section_f3d27369e0f54164aa5e9b1abda16f5f)

  WBXML code page 3: AirNotify            (no longer used)

  WBXML code page 4: Calendar             [\[MS-ASCAL\]](%5bMS-ASCAL%5d.pdf#Section_0c4486824a6a459aae662fed0712bef9)

  WBXML code page 5: Move                 \[MS-ASCMD\] section 2.2.1.12

  WBXML code page 6: GetItemEstimate      \[MS-ASCMD\] section 2.2.1.9

  WBXML code page 7: FolderHierarchy      \[MS-ASCMD\] sections 2.2.1.3, 2.2.1.4, 2.2.1.5, 2.2.1.6, and 2.2.1.8

  WBXML code page 8: MeetingResponse      \[MS-ASCMD\] section 2.2.1.11

  WBXML code page 9: Tasks                [\[MS-ASTASK\]](%5bMS-ASTASK%5d.pdf#Section_b8fe266450ba4d00bf6be4deab352c89)

  WBXML code page 10: ResolveRecipients   \[MS-ASCMD\] section 2.2.1.15

  WBXML code page 11: ValidateCert        \[MS-ASCMD\] section 2.2.1.22

  WBXML code page 12: Contacts2           \[MS-ASCNTC\]

  WBXML code page 13: Ping                \[MS-ASCMD\] section 2.2.1.13

  WBXML code page 14: Provision           [\[MS-ASPROV\]](%5bMS-ASPROV%5d.pdf#Section_449c453b74d74919bfe895972b27048a)

  WBXML code page 15: Search              \[MS-ASCMD\] section 2.2.1.16

  WBXML code page 16: GAL                 \[MS-ASCMD\] section 2.2.1.16

  WBXML code page 17: AirSyncBase         [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c)

  WBXML code page 18: Settings            \[MS-ASCMD\] section 2.2.1.18

  WBXML code page 19: DocumentLibrary     [\[MS-ASDOC\]](%5bMS-ASDOC%5d.pdf#Section_c503701c0e594beb9b8b038cd69a3443)

  WBXML code page 20: ItemOperations      \[MS-ASCMD\] sections 2.2.1.10

  WBXML code page 21: ComposeMail         \[MS-ASCMD\] sections 2.2.1.17, 2.2.1.19, and 2.2.1.20

  WBXML code page 22: Email2              \[MS-ASEMAIL\]

  WBXML code page 23: Notes               [\[MS-ASNOTE\]](%5bMS-ASNOTE%5d.pdf#Section_21801d6c000e413c859150430a8e9fd9)

  WBXML code page 24: Rights Management   [\[MS-ASRM\]](%5bMS-ASRM%5d.pdf#Section_71e681b7e1784c1096b678df7fa77dfc)

  WBXML code page 25: Find                \[MS-ASCMD\] section 2.2.1.2
  --------------------------------------------------------------------------------------------------------------------------------------

For conceptual background information and overviews of the relationships and interactions between this and other protocols, see [\[MS-OXPROTO\]](%5bMS-OXPROTO%5d.pdf#Section_734ab967e43e425babe1974af56c0283).

## Applicability Statement

[**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc) is used as a compact transmission format for mobile clients, which typically have little memory and bandwidth. WBXML encoding of [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) tags is appropriate for all messages sent and received by using the ActiveSync protocols.

## Standards Assignments

This algorithm uses the global [**WBXML tokens**](#gt_ec12fbbc-e750-435b-bc6e-919038ba9127) listed in the following table.

  ---------------------------------------------------------------------------------------------------------------
  Token name                    Token            Reference
  ----------------------------- ---------------- ----------------------------------------------------------------
  SWITCH_PAGE                   0                [\[WBXML1.2\]](https://go.microsoft.com/fwlink/?LinkId=160492)

  END                           1                \[WBXML1.2\]

  ENTITY                        2                \[WBXML1.2\]

  STR_I                         3                \[WBXML1.2\]

  LITERAL                       4                \[WBXML1.2\]

  EXT_I_0                       40               \[WBXML1.2\]

  EXT_I_1                       41               \[WBXML1.2\]

  EXT_I_2                       42               \[WBXML1.2\]

  PI                            43               \[WBXML1.2\]

  LITERAL_C                     44               \[WBXML1.2\]

  EXT_T_0                       80               \[WBXML1.2\]

  EXT_T_1                       81               \[WBXML1.2\]

  EXT_T_2                       82               \[WBXML1.2\]

  STR_T                         83               \[WBXML1.2\]

  LITERAL_A                     84               \[WBXML1.2\]

  EXT_0                         C0               \[WBXML1.2\]

  EXT_1                         C1               \[WBXML1.2\]

  EXT_2                         C2               \[WBXML1.2\]

  OPAQUE                        C3               \[WBXML1.2\]

  LITERAL_AC                    C4               \[WBXML1.2\]
  ---------------------------------------------------------------------------------------------------------------

# Algorithm Details

## ActiveSync WBXML Algorithm Details

ActiveSync messages are transported as HTTP **POST** messages, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d), where the body of the message contains [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc) formatted data. The WBXML data encodes the [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) required by the command being communicated in the message. The ActiveSync commands are specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

This section specifies the algorithm to encode XML request and response messages into WBXML format and to decode WBXML data into XML request and response messages. This algorithm is used by clients and servers.

### Abstract Data Model

None.

### Initialization

The [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) tags in both request and response messages are encoded by using [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc) tokenization, as specified in [\[WBXML1.2\]](https://go.microsoft.com/fwlink/?LinkId=160492). WBXML uses [**WBXML code pages**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) to map XML tags to [**WBXML tokens**](#gt_ec12fbbc-e750-435b-bc6e-919038ba9127). WBXML parsers MUST use the WBXML code pages specified in the following sections.

#### Code Pages

This algorithm supports the following [**WBXML code pages**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1).

  -----------------------------------------------------------------------
  Code page                    XML namespace
  ---------------------------- ------------------------------------------
  0                            AirSync

  1                            Contacts

  2                            Email

  3                            AirNotify

  4                            Calendar

  5                            Move

  6                            GetItemEstimate

  7                            FolderHierarchy

  8                            MeetingResponse

  9                            Tasks

  10                           ResolveRecipients

  11                           ValidateCert

  12                           Contacts2

  13                           Ping

  14                           Provision

  15                           Search

  16                           Gal

  17                           AirSyncBase

  18                           Settings

  19                           DocumentLibrary

  20                           ItemOperations

  21                           ComposeMail

  22                           Email2

  23                           Notes

  24                           RightsManagement

  25                           Find
  -----------------------------------------------------------------------

##### Code Page 0: AirSync

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 0 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  ----------------------------------------------------------------------------------
  Tag name                     Token                  Protocol versions
  ---------------------------- ---------------------- ------------------------------
  **Sync**                     0x05                   All

  **Responses**                0x06                   All

  **Add**                      0x07                   All

  **Change**                   0x08                   All

  **Delete**                   0x09                   All

  **Fetch**                    0x0A                   All

  **SyncKey**                  0x0B                   All

  **ClientId**                 0x0C                   All

  **ServerId**                 0x0D                   All

  **Status**                   0x0E                   All

  **Collection**               0x0F                   All

  **Class**                    0x10                   All

  **CollectionId**             0x12                   All

  **GetChanges**               0x13                   All

  **MoreAvailable**            0x14                   All

  **WindowSize**               0x15                   All

  **Commands**                 0x16                   All

  **Options**                  0x17                   All

  **FilterType**               0x18                   All

  **Truncation**               0x19                   2.5

  **Conflict**                 0x1B                   All

  **Collections**              0x1C                   All

  **ApplicationData**          0x1D                   All

  **DeletesAsMoves**           0x1E                   All

  **Supported**                0x20                   All

  **SoftDelete**               0x21                   All

  **MIMESupport**              0x22                   All

  **MIMETruncation**           0x23                   All

  **Wait**                     0x24                   12.1, 14.0, 14.1, 16.0, 16.1

  **Limit**                    0x25                   12.1, 14.0, 14.1, 16.0, 16.1

  **Partial**                  0x26                   12.1, 14.0, 14.1, 16.0, 16.1

  **ConversationMode**         0x27                   14.0, 14.1, 16.0, 16.1

  **MaxItems**                 0x28                   14.0, 14.1, 16.0, 16.1

  **HeartbeatInterval**        0x29                   14.0, 14.1, 16.0, 16.1
  ----------------------------------------------------------------------------------

##### Code Page 1: Contacts

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 1 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  ---------------------------------------------------------------------------------------------
  Tag name                                       Token                 Protocol versions
  ---------------------------------------------- --------------------- ------------------------
  **Anniversary**                                0x05                  All

  **AssistantName**                              0x06                  All

  **AssistantPhoneNumber**                       0x07                  All

  **Birthday**                                   0x08                  All

  **Body** --- see note 1 following this table   0x09                  2.5

  **BodySize**                                   0x0A                  2.5

  **BodyTruncated**                              0x0B                  2.5

  **Business2PhoneNumber**                       0x0C                  All

  **BusinessAddressCity**                        0x0D                  All

  **BusinessAddressCountry**                     0x0E                  All

  **BusinessAddressPostalCode**                  0x0F                  All

  **BusinessAddressState**                       0x10                  All

  **BusinessAddressStreet**                      0x11                  All

  **BusinessFaxNumber**                          0x12                  All

  **BusinessPhoneNumber**                        0x13                  All

  **CarPhoneNumber**                             0x14                  All

  **Categories**                                 0x15                  All

  **Category**                                   0x16                  All

  **Children**                                   0x17                  All

  **Child**                                      0x18                  All

  **CompanyName**                                0x19                  All

  **Department**                                 0x1A                  All

  **Email1Address**                              0x1B                  All

  **Email2Address**                              0x1C                  All

  **Email3Address**                              0x1D                  All

  **FileAs**                                     0x1E                  All

  **FirstName**                                  0x1F                  All

  **Home2PhoneNumber**                           0x20                  All

  **HomeAddressCity**                            0x21                  All

  **HomeAddressCountry**                         0x22                  All

  **HomeAddressPostalCode**                      0x23                  All

  **HomeAddressState**                           0x24                  All

  **HomeAddressStreet**                          0x25                  All

  **HomeFaxNumber**                              0x26                  All

  **HomePhoneNumber**                            0x27                  All

  **JobTitle**                                   0x28                  All

  **LastName**                                   0x29                  All

  **MiddleName**                                 0x2A                  All

  **MobilePhoneNumber**                          0x2B                  All

  **OfficeLocation**                             0x2C                  All

  **OtherAddressCity**                           0x2D                  All

  **OtherAddressCountry**                        0x2E                  All

  **OtherAddressPostalCode**                     0x2F                  All

  **OtherAddressState**                          0x30                  All

  **OtherAddressStreet**                         0x31                  All

  **PagerNumber**                                0x32                  All

  **RadioPhoneNumber**                           0x33                  All

  **Spouse**                                     0x34                  All

  **Suffix**                                     0x35                  All

  **Title**                                      0x36                  All

  **WebPage**                                    0x37                  All

  **YomiCompanyName**                            0x38                  All

  **YomiFirstName**                              0x39                  All

  **YomiLastName**                               0x3A                  All

  **Picture**                                    0x3C                  All

  **Alias**                                      0x3D                  14.0, 14.1, 16.0, 16.1

  **WeightedRank**                               0x3E                  14.0, 14.1, 16.0, 16.1
  ---------------------------------------------------------------------------------------------

**Note 1:** The **Body** tag in WBXML code page 17 (AirSyncBase) is used instead of the **Body** tag in WBXML code page 1 with protocol versions 12.0, 12.1, 14.0, 14.1, 16.0, and 16.1.

##### Code Page 2: Email

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 2 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  ------------------------------------------------------------------------------------------------------
  Tag name                                              Token       Protocol versions
  ----------------------------------------------------- ----------- ------------------------------------
  **Attachment**                                        0x05        2.5

  **Attachments** --- see note 1 following this table   0x06        2.5

  **AttName**                                           0x07        2.5

  **AttSize**                                           0x08        2.5

  **Att0id**                                            0x09        2.5

  **AttMethod**                                         0x0A        2.5

  **Body** --- see note 2 following this table          0x0C        2.5

  **BodySize**                                          0x0D        2.5

  **BodyTruncated**                                     0x0E        2.5

  **DateReceived**                                      0x0F        All

  **DisplayName**                                       0x10        2.5

  **DisplayTo**                                         0x11        All

  **Importance**                                        0x12        All

  **MessageClass**                                      0x13        All

  **Subject**                                           0x14        All

  **Read**                                              0x15        All

  **To**                                                0x16        All

  **Cc**                                                0x17        All

  **From**                                              0x18        All

  **ReplyTo**                                           0x19        All

  **AllDayEvent**                                       0x1A        All

  **Categories**                                        0x1B        14.0, 14.1, 16.0, 16.1

  **Category**                                          0x1C        14.0, 14.1, 16.0, 16.1

  **DtStamp**                                           0x1D        All

  **EndTime**                                           0x1E        All

  **InstanceType**                                      0x1F        All

  **BusyStatus**                                        0x20        All

  **Location** --- see note 3 following this table      0x21        2.5, 12.0, 12.1, 14.0, 14.1

  **MeetingRequest**                                    0x22        All

  **Organizer**                                         0x23        All

  **RecurrenceId**                                      0x24        All

  **Reminder**                                          0x25        All

  **ResponseRequested**                                 0x26        All

  **Recurrences**                                       0x27        All

  **Recurrence**                                        0x28        All

  **Type**                                              0x29        All

  **Until**                                             0x2A        All

  **Occurrences**                                       0x2B        All

  **Interval**                                          0x2C        All

  **DayOfWeek**                                         0x2D        All

  **DayOfMonth**                                        0x2E        All

  **WeekOfMonth**                                       0x2F        All

  **MonthOfYear**                                       0x30        All

  **StartTime**                                         0x31        All

  **Sensitivity**                                       0x32        All

  **TimeZone**                                          0x33        All

  **GlobalObjId** --- see note 4 following this table   0x34        2.5, 12.0, 12.1, 14.0, 14.1

  **ThreadTopic**                                       0x35        All

  **MIMEData**                                          0x36        2.5

  **MIMETruncated**                                     0x37        2.5

  **MIMESize**                                          0x38        2.5

  **InternetCPID**                                      0x39        All

  **Flag**                                              0x3A        12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Status**                                            0x3B        12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **ContentClass**                                      0x3C        12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **FlagType**                                          0x3D        12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **CompleteTime**                                      0x3E        12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **DisallowNewTimeProposal**                           0x3F        14.0, 14.1, 16.0, 16.1
  ------------------------------------------------------------------------------------------------------

**Note 1:** The **Attachments** tag in WBXML code page 17 (AirSyncBase) is used instead of the **Attachments** tag in WBXML code page 2 with protocol versions 12.0, 12.1, 14.0, 14.1, 16.0, and 16.1.

**Note 2:** The **Body** tag in WBXML code page 17 (AirSyncBase) is used instead of the **Body** tag in WBXML code page 2 with protocol versions 12.0, 12.1, 14.0, 14.1, 16.0, and 16.1.

**Note 3:** The **Location** tag in WBXML code page 17 (AirSyncBase) is used instead of the **Location** tag in WBXML code page 2 with protocol version 16.0 and 16.1.

**Note 4:** The **UID** tag in WBXML code page 4 (Calendar) is used instead of the **GlobalObjId** tag in WBXML code page 2 with protocol version 16.0 and 16.1.

##### Code Page 3: AirNotify

[**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 3 is no longer in use.

##### Code Page 4: Calendar

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 4 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  -------------------------------------------------------------------------------------------------
  Tag name                                           Token     Protocol versions
  -------------------------------------------------- --------- ------------------------------------
  **Timezone**                                       0x05      All

  **AllDayEvent**                                    0x06      All

  **Attendees**                                      0x07      All

  **Attendee**                                       0x08      All

  **Email**                                          0x09      All

  **Name**                                           0x0A      All

  **Body** --- see note 1 following this table       0x0B      2.5

  **BodyTruncated**                                  0x0C      2.5

  **BusyStatus**                                     0x0D      All

  **Categories**                                     0x0E      All

  **Category**                                       0x0F      All

  **DtStamp**                                        0x11      All

  **EndTime**                                        0x12      All

  **Exception**                                      0x13      All

  **Exceptions**                                     0x14      All

  **Deleted**                                        0x15      All

  **ExceptionStartTime**                             0x16      2.5, 12.0, 12.1, 14.0, 14.1

  **Location** --- see note 2 following this table   0x17      2.5, 12.0, 12.1, 14.0, 14.1

  **MeetingStatus**                                  0x18      All

  **OrganizerEmail**                                 0x19      All

  **OrganizerName**                                  0x1A      All

  **Recurrence**                                     0x1B      All

  **Type**                                           0x1C      All

  **Until**                                          0x1D      All

  **Occurrences**                                    0x1E      All

  **Interval**                                       0x1F      All

  **DayOfWeek**                                      0x20      All

  **DayOfMonth**                                     0x21      All

  **WeekOfMonth**                                    0x22      All

  **MonthOfYear**                                    0x23      All

  **Reminder**                                       0x24      All

  **Sensitivity**                                    0x25      All

  **Subject**                                        0x26      All

  **StartTime**                                      0x27      All

  **UID**                                            0x28      All

  **AttendeeStatus**                                 0x29      12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **AttendeeType**                                   0x2A      12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **DisallowNewTimeProposal**                        0x33      14.0, 14.1, 16.0, 16.1

  **ResponseRequested**                              0x34      14.0, 14.1, 16.0, 16.1

  **AppointmentReplyTime**                           0x35      14.0, 14.1, 16.0, 16.1

  **ResponseType**                                   0x36      14.0, 14.1, 16.0, 16.1

  **CalendarType**                                   0x37      14.0, 14.1, 16.0, 16.1

  **IsLeapMonth**                                    0x38      14.0, 14.1, 16.0, 16.1

  **FirstDayOfWeek**                                 0x39      14.1, 16.0, 16.1

  **OnlineMeetingConfLink**                          0x3A      14.1, 16.0, 16.1

  **OnlineMeetingExternalLink**                      0x3B      14.1, 16.0, 16.1

  **ClientUid**                                      0x3C      16.0, 16.1
  -------------------------------------------------------------------------------------------------

**Note 1:** The **Body** tag in WBXML code page 17 (AirSyncBase) is used instead of the **Body** tag in WBXML code page 4 with protocol versions 12.0, 12.1, 14.0, 14.1, 16.0, and 16.1.

**Note 2:** The **Location** tag in WBXML code page 17 (AirSyncBase) is used instead of the **Location** tag in WBXML code page 4 with protocol version 16.0 and 16.1.

##### Code Page 5: Move

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 5 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  ------------------------------------------------------------------------
  Tag name                                   Token     Protocol versions
  ------------------------------------------ --------- -------------------
  **MoveItems**                              0x05      All

  **Move**                                   0x06      All

  **SrcMsgId**                               0x07      All

  **SrcFldId**                               0x08      All

  **DstFldId**                               0x09      All

  **Response**                               0x0A      All

  **Status**                                 0x0B      All

  **DstMsgId**                               0x0C      All
  ------------------------------------------------------------------------

##### Code Page 6: GetItemEstimate

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 6 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  -----------------------------------------------------------------------------
  Tag name                                        Token     Protocol versions
  ----------------------------------------------- --------- -------------------
  **GetItemEstimate**                             0x05      All

  **Collections**                                 0x07      All

  **Collection**                                  0x08      All

  **Class** --- see note 1 following this table   0x09      2.5, 12.0, 12.1

  **CollectionId**                                0x0A      All

  **Estimate**                                    0x0C      All

  **Response**                                    0x0D      All

  **Status**                                      0x0E      All
  -----------------------------------------------------------------------------

**Note 1:** The **Class** tag in WBXML code page 0 (AirSync) is used instead of the **Class** tag in WBXML code page 6 with protocol versions 14.0, 14.1, 16.0, and 16.1.

##### Code Page 7: FolderHierarchy

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 7 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  ------------------------------------------------------------------------
  Tag name                                   Token     Protocol versions
  ------------------------------------------ --------- -------------------
  **Folders**                                0x05      2.5, 12.0, 12.1

  **Folder**                                 0x06      2.5, 12.0, 12.1

  **DisplayName**                            0x07      All

  **ServerId**                               0x08      All

  **ParentId**                               0x09      All

  **Type**                                   0x0A      All

  **Status**                                 0x0C      All

  **Changes**                                0x0E      All

  **Add**                                    0x0F      All

  **Delete**                                 0x10      All

  **Update**                                 0x11      All

  **SyncKey**                                0x12      All

  **FolderCreate**                           0x13      All

  **FolderDelete**                           0x14      All

  **FolderUpdate**                           0x15      All

  **FolderSync**                             0x16      All

  **Count**                                  0x17      All
  ------------------------------------------------------------------------

##### Code Page 8: MeetingResponse

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 8 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  ------------------------------------------------------------------------
  Tag name                                   Token     Protocol versions
  ------------------------------------------ --------- -------------------
  **CalendarId**                             0x05      All

  **CollectionId**                           0x06      All

  **MeetingResponse**                        0x07      All

  **RequestId**                              0x08      All

  **Request**                                0x09      All

  **Result**                                 0x0A      All

  **Status**                                 0x0B      All

  **UserResponse**                           0x0C      All

  **InstanceId**                             0x0E      14.1, 16.0, 16.1

  **ProposedStartTime**                      0x10      16.1

  **ProposedEndTime**                        0x11      16.1

  **SendResponse**                           0x12      16.0, 16.1
  ------------------------------------------------------------------------

##### Code Page 9: Tasks

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 9 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  --------------------------------------------------------------------------------------------
  Tag name                                       Token    Protocol versions
  ---------------------------------------------- -------- ------------------------------------
  **Body** --- see note 1 following this table   0x05     2.5

  **BodySize**                                   0x06     2.5

  **BodyTruncated**                              0x07     2.5

  **Categories**                                 0x08     All

  **Category**                                   0x09     All

  **Complete**                                   0x0A     All

  **DateCompleted**                              0x0B     All

  **DueDate**                                    0x0C     All

  **UtcDueDate**                                 0x0D     All

  **Importance**                                 0x0E     All

  **Recurrence**                                 0x0F     All

  **Type**                                       0x10     All

  **Start**                                      0x11     All

  **Until**                                      0x12     All

  **Occurrences**                                0x13     All

  **Interval**                                   0x14     All

  **DayOfMonth**                                 0x15     All

  **DayOfWeek**                                  0x16     All

  **WeekOfMonth**                                0x17     All

  **MonthOfYear**                                0x18     All

  **Regenerate**                                 0x19     All

  **DeadOccur**                                  0x1A     All

  **ReminderSet**                                0x1B     All

  **ReminderTime**                               0x1C     All

  **Sensitivity**                                0x1D     All

  **StartDate**                                  0x1E     All

  **UtcStartDate**                               0x1F     All

  **Subject**                                    0x20     All

  **OrdinalDate**                                0x22     12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **SubOrdinalDate**                             0x23     12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **CalendarType**                               0x24     14.0, 14.1, 16.0, 16.1

  **IsLeapMonth**                                0x25     14.0, 14.1, 16.0, 16.1

  **FirstDayOfWeek**                             0x26     14.1, 16.0, 16.1
  --------------------------------------------------------------------------------------------

**Note 1:** The **Body** tag in WBXML code page 17 (AirSyncBase) is used instead of the **Body** tag in WBXML code page 9 with protocol versions 12.0, 12.1, 14.0, 14.1, 16.0, and 16.1.

##### Code Page 10: ResolveRecipients

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 10 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  ------------------------------------------------------------------------------
  Tag name                                   Token      Protocol versions
  ------------------------------------------ ---------- ------------------------
  **ResolveRecipients**                      0x05       All

  **Response**                               0x06       All

  **Status**                                 0x07       All

  **Type**                                   0x08       All

  **Recipient**                              0x09       All

  **DisplayName**                            0x0A       All

  **EmailAddress**                           0x0B       All

  **Certificates**                           0x0C       All

  **Certificate**                            0x0D       All

  **MiniCertificate**                        0x0E       All

  **Options**                                0x0F       All

  **To**                                     0x10       All

  **CertificateRetrieval**                   0x11       All

  **RecipientCount**                         0x12       All

  **MaxCertificates**                        0x13       All

  **MaxAmbiguousRecipients**                 0x14       All

  **CertificateCount**                       0x15       All

  **Availability**                           0x16       14.0, 14.1, 16.0, 16.1

  **StartTime**                              0x17       14.0, 14.1, 16.0, 16.1

  **EndTime**                                0x18       14.0, 14.1, 16.0, 16.1

  **MergedFreeBusy**                         0x19       14.0, 14.1, 16.0, 16.1

  **Picture**                                0x1A       14.1, 16.0, 16.1

  **MaxSize**                                0x1B       14.1, 16.0, 16.1

  **Data**                                   0x1C       14.1, 16.0, 16.1

  **MaxPictures**                            0x1D       14.1, 16.0, 16.1
  ------------------------------------------------------------------------------

##### Code Page 11: ValidateCert

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 11 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  -------------------------------------------------------------------------
  Tag name                                   Token      Protocol versions
  ------------------------------------------ ---------- -------------------
  **ValidateCert**                           0x05       All

  **Certificates**                           0x06       All

  **Certificate**                            0x07       All

  **CertificateChain**                       0x08       All

  **CheckCRL**                               0x09       All

  **Status**                                 0x0A       All
  -------------------------------------------------------------------------

##### Code Page 12: Contacts2

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 12 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  -------------------------------------------------------------------------
  Tag name                                   Token      Protocol versions
  ------------------------------------------ ---------- -------------------
  **CustomerId**                             0x05       All

  **GovernmentId**                           0x06       All

  **IMAddress**                              0x07       All

  **IMAddress2**                             0x08       All

  **IMAddress3**                             0x09       All

  **ManagerName**                            0x0A       All

  **CompanyMainPhone**                       0x0B       All

  **AccountName**                            0x0C       All

  **NickName**                               0x0D       All

  **MMS**                                    0x0E       All
  -------------------------------------------------------------------------

##### Code Page 13: Ping

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 13 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  -------------------------------------------------------------------------
  Tag name                                   Token      Protocol versions
  ------------------------------------------ ---------- -------------------
  **Ping**                                   0x05       All

  **Status**                                 0x07       All

  **HeartbeatInterval**                      0x08       All

  **Folders**                                0x09       All

  **Folder**                                 0x0A       All

  **Id**                                     0x0B       All

  **Class**                                  0x0C       All

  **MaxFolders**                             0x0D       All
  -------------------------------------------------------------------------

##### Code Page 14: Provision

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 14 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  ---------------------------------------------------------------------------------------------
  Tag name                                       Token     Protocol versions
  ---------------------------------------------- --------- ------------------------------------
  **Provision**                                  0x05      All

  **Policies**                                   0x06      All

  **Policy**                                     0x07      All

  **PolicyType**                                 0x08      All

  **PolicyKey**                                  0x09      All

  **Data**                                       0x0A      All

  **Status**                                     0x0B      All

  **RemoteWipe**                                 0x0C      All

  **EASProvisionDoc**                            0x0D      12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **DevicePasswordEnabled**                      0x0E      12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **AlphanumericDevicePasswordRequired**         0x0F      12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **RequireStorageCardEncryption**               0x10      12.1, 14.0, 14.1, 16.0, 16.1

  **PasswordRecoveryEnabled**                    0x11      12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **AttachmentsEnabled**                         0x13      12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **MinDevicePasswordLength**                    0x14      12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **MaxInactivityTimeDeviceLock**                0x15      12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **MaxDevicePasswordFailedAttempts**            0x16      12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **MaxAttachmentSize**                          0x17      12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **AllowSimpleDevicePassword**                  0x18      12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **DevicePasswordExpiration**                   0x19      12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **DevicePasswordHistory**                      0x1A      12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **AllowStorageCard**                           0x1B      12.1, 14.0, 14.1, 16.0, 16.1

  **AllowCamera**                                0x1C      12.1, 14.0, 14.1, 16.0, 16.1

  **RequireDeviceEncryption**                    0x1D      12.1, 14.0, 14.1, 16.0, 16.1

  **AllowUnsignedApplications**                  0x1E      12.1, 14.0, 14.1, 16.0, 16.1

  **AllowUnsignedInstallationPackages**          0x1F      12.1, 14.0, 14.1, 16.0, 16.1

  **MinDevicePasswordComplexCharacters**         0x20      12.1, 14.0, 14.1, 16.0, 16.1

  **AllowWiFi**                                  0x21      12.1, 14.0, 14.1, 16.0, 16.1

  **AllowTextMessaging**                         0x22      12.1, 14.0, 14.1, 16.0, 16.1

  **AllowPOPIMAPEmail**                          0x23      12.1, 14.0, 14.1, 16.0, 16.1

  **AllowBluetooth**                             0x24      12.1, 14.0, 14.1, 16.0, 16.1

  **AllowIrDA**                                  0x25      12.1, 14.0, 14.1, 16.0, 16.1

  **RequireManualSyncWhenRoaming**               0x26      12.1, 14.0, 14.1, 16.0, 16.1

  **AllowDesktopSync**                           0x27      12.1, 14.0, 14.1, 16.0, 16.1

  **MaxCalendarAgeFilter**                       0x28      12.1, 14.0, 14.1, 16.0, 16.1

  **AllowHTMLEmail**                             0x29      12.1, 14.0, 14.1, 16.0, 16.1

  **MaxEmailAgeFilter**                          0x2A      12.1, 14.0, 14.1, 16.0, 16.1

  **MaxEmailBodyTruncationSize**                 0x2B      12.1, 14.0, 14.1, 16.0, 16.1

  **MaxEmailHTMLBodyTruncationSize**             0x2C      12.1, 14.0, 14.1, 16.0, 16.1

  **RequireSignedSMIMEMessages**                 0x2D      12.1, 14.0, 14.1, 16.0, 16.1

  **RequireEncryptedSMIMEMessages**              0x2E      12.1, 14.0, 14.1, 16.0, 16.1

  **RequireSignedSMIMEAlgorithm**                0x2F      12.1, 14.0, 14.1, 16.0, 16.1

  **RequireEncryptionSMIMEAlgorithm**            0x30      12.1, 14.0, 14.1, 16.0, 16.1

  **AllowSMIMEEncryptionAlgorithmNegotiation**   0x31      12.1, 14.0, 14.1, 16.0, 16.1

  **AllowSMIMESoftCerts**                        0x32      12.1, 14.0, 14.1, 16.0, 16.1

  **AllowBrowser**                               0x33      12.1, 14.0, 14.1, 16.0, 16.1

  **AllowConsumerEmail**                         0x34      12.1, 14.0, 14.1, 16.0, 16.1

  **AllowRemoteDesktop**                         0x35      12.1, 14.0, 14.1, 16.0, 16.1

  **AllowInternetSharing**                       0x36      12.1, 14.0, 14.1, 16.0, 16.1

  **UnapprovedInROMApplicationList**             0x37      12.1, 14.0, 14.1, 16.0, 16.1

  **ApplicationName**                            0x38      12.1, 14.0, 14.1, 16.0, 16.1

  **ApprovedApplicationList**                    0x39      12.1, 14.0, 14.1, 16.0, 16.1

  **Hash**                                       0x3A      12.1, 14.0, 14.1, 16.0, 16.1

  **AccountOnlyRemoteWipe**                      0x3B      16.1
  ---------------------------------------------------------------------------------------------

##### Code Page 15: Search

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 15 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  --------------------------------------------------------------------------------------
  Tag name                              Token       Protocol versions
  ------------------------------------- ----------- ------------------------------------
  **Search**                            0x05        All

  **Store**                             0x07        All

  **Name**                              0x08        All

  **Query**                             0x09        All

  **Options**                           0x0A        All

  **Range**                             0x0B        All

  **Status**                            0x0C        All

  **Response**                          0x0D        All

  **Result**                            0x0E        All

  **Properties**                        0x0F        All

  **Total**                             0x10        All

  **EqualTo**                           0x11        12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Value**                             0x12        12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **And**                               0x13        12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Or**                                0x14        12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **FreeText**                          0x15        12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **DeepTraversal**                     0x17        12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **LongId**                            0x18        12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **RebuildResults**                    0x19        12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **LessThan**                          0x1A        12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **GreaterThan**                       0x1B        12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **UserName**                          0x1E        12.1, 14.0, 14.1, 16.0, 16.1

  **Password**                          0x1F        12.1, 14.0, 14.1, 16.0, 16.1

  **ConversationId**                    0x20        14.0, 14.1, 16.0, 16.1

  **Picture**                           0x21        14.1, 16.0, 16.1

  **MaxSize**                           0x22        14.1, 16.0, 16.1

  **MaxPictures**                       0x23        14.1, 16.0, 16.1
  --------------------------------------------------------------------------------------

##### Code Page 16: GAL

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 16 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  -------------------------------------------------------------------------
  Tag name                                   Token      Protocol versions
  ------------------------------------------ ---------- -------------------
  **DisplayName**                            0x05       All

  **Phone**                                  0x06       All

  **Office**                                 0x07       All

  **Title**                                  0x08       All

  **Company**                                0x09       All

  **Alias**                                  0x0A       All

  **FirstName**                              0x0B       All

  **LastName**                               0x0C       All

  **HomePhone**                              0x0D       All

  **MobilePhone**                            0x0E       All

  **EmailAddress**                           0x0F       All

  **Picture**                                0x10       14.1, 16.0, 16.1

  **Status**                                 0x11       14.1, 16.0, 16.1

  **Data**                                   0x12       14.1, 16.0, 16.1
  -------------------------------------------------------------------------

##### Code Page 17: AirSyncBase

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 17 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  ------------------------------------------------------------------------------------
  Tag name                             Token      Protocol versions
  ------------------------------------ ---------- ------------------------------------
  **BodyPreference**                   0x05       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Type**                             0x06       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **TruncationSize**                   0x07       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **AllOrNone**                        0x08       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Body**                             0x0A       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Data**                             0x0B       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **EstimatedDataSize**                0x0C       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Truncated**                        0x0D       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Attachments**                      0x0E       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Attachment**                       0x0F       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **DisplayName**                      0x10       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **FileReference**                    0x11       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Method**                           0x12       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **ContentId**                        0x13       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **ContentLocation**                  0x14       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **IsInline**                         0x15       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **NativeBodyType**                   0x16       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **ContentType**                      0x17       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Preview**                          0x18       14.0, 14.1, 16.0, 16.1

  **BodyPartPreference**               0x19       14.1, 16.0, 16.1

  **BodyPart**                         0x1A       14.1, 16.0, 16.1

  **Status**                           0x1B       14.1, 16.0, 16.1

  **Add**                              0x1C       16.0, 16.1

  **Delete**                           0x1D       16.0, 16.1

  **ClientId**                         0x1E       16.0, 16.1

  **Content**                          0x1F       16.0, 16.1

  **Location**                         0x20       16.0, 16.1

  **Annotation**                       0x21       16.0, 16.1

  **Street**                           0x22       16.0, 16.1

  **City**                             0x23       16.0, 16.1

  **State**                            0x24       16.0, 16.1

  **Country**                          0x25       16.0, 16.1

  **PostalCode**                       0x26       16.0, 16.1

  **Latitude**                         0x27       16.0, 16.1

  **Longitude**                        0x28       16.0, 16.1

  **Accuracy**                         0x29       16.0, 16.1

  **Altitude**                         0x2A       16.0, 16.1

  **AltitudeAccuracy**                 0x2B       16.0, 16.1

  **LocationUri**                      0x2C       16.0, 16.1

  **InstanceId**                       0x2D       16.0, 16.1
  ------------------------------------------------------------------------------------

##### Code Page 18: Settings

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 18 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  --------------------------------------------------------------------------------------
  Tag name                               Token      Protocol versions
  -------------------------------------- ---------- ------------------------------------
  **Settings**                           0x05       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Status**                             0x06       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Get**                                0x07       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Set**                                0x08       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Oof**                                0x09       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **OofState**                           0x0A       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **StartTime**                          0x0B       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **EndTime**                            0x0C       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **OofMessage**                         0x0D       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **AppliesToInternal**                  0x0E       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **AppliesToExternalKnown**             0x0F       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **AppliesToExternalUnknown**           0x10       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Enabled**                            0x11       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **ReplyMessage**                       0x12       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **BodyType**                           0x13       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **DevicePassword**                     0x14       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Password**                           0x15       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **DeviceInformation**                  0x16       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Model**                              0x17       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **IMEI**                               0x18       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **FriendlyName**                       0x19       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **OS**                                 0x1A       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **OSLanguage**                         0x1B       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **PhoneNumber**                        0x1C       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **UserInformation**                    0x1D       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **EmailAddresses**                     0x1E       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **SMTPAddress**                        0x1F       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **UserAgent**                          0x20       12.1, 14.0, 14.1, 16.0, 16.1

  **EnableOutboundSMS**                  0x21       14.0, 14.1, 16.0, 16.1

  **MobileOperator**                     0x22       14.0, 14.1, 16.0, 16.1

  **PrimarySmtpAddress**                 0x23       14.1, 16.0, 16.1

  **Accounts**                           0x24       14.1, 16.0, 16.1

  **Account**                            0x25       14.1, 16.0, 16.1

  **AccountId**                          0x26       14.1, 16.0, 16.1

  **AccountName**                        0x27       14.1, 16.0, 16.1

  **UserDisplayName**                    0x28       14.1, 16.0, 16.1

  **SendDisabled**                       0x29       14.1, 16.0, 16.1

  **RightsManagementInformation**        0x2B       14.1, 16.0, 16.1
  --------------------------------------------------------------------------------------

##### Code Page 19: DocumentLibrary

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 19 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  ---------------------------------------------------------------------------------------
  Tag name                                Token      Protocol versions
  --------------------------------------- ---------- ------------------------------------
  **LinkId**                              0x05       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **DisplayName**                         0x06       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **IsFolder**                            0x07       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **CreationDate**                        0x08       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **LastModifiedDate**                    0x09       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **IsHidden**                            0x0A       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **ContentLength**                       0x0B       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **ContentType**                         0x0C       12.0, 12.1, 14.0, 14.1, 16.0, 16.1
  ---------------------------------------------------------------------------------------

##### Code Page 20: ItemOperations

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 20 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  ---------------------------------------------------------------------------------------
  Tag name                                Token      Protocol versions
  --------------------------------------- ---------- ------------------------------------
  **ItemOperations**                      0x05       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Fetch**                               0x06       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Store**                               0x07       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Options**                             0x08       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Range**                               0x09       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Total**                               0x0A       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Properties**                          0x0B       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Data**                                0x0C       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Status**                              0x0D       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Response**                            0x0E       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Version**                             0x0F       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Schema**                              0x10       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **Part**                                0x11       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **EmptyFolderContents**                 0x12       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **DeleteSubFolders**                    0x13       12.0, 12.1, 14.0, 14.1, 16.0, 16.1

  **UserName**                            0x14       12.1, 14.0, 14.1, 16.0, 16.1

  **Password**                            0x15       12.1, 14.0, 14.1, 16.0, 16.1

  **Move**                                0x16       14.0, 14.1, 16.0, 16.1

  **DstFldId**                            0x17       14.0, 14.1, 16.0, 16.1

  **ConversationId**                      0x18       14.0, 14.1, 16.0, 16.1

  **MoveAlways**                          0x19       14.0, 14.1, 16.0, 16.1
  ---------------------------------------------------------------------------------------

##### Code Page 21: ComposeMail

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 21 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  ------------------------------------------------------------------------------
  Tag name                                   Token      Protocol versions
  ------------------------------------------ ---------- ------------------------
  **SendMail**                               0x05       14.0, 14.1, 16.0, 16.1

  **SmartForward**                           0x06       14.0, 14.1, 16.0, 16.1

  **SmartReply**                             0x07       14.0, 14.1, 16.0, 16.1

  **SaveInSentItems**                        0x08       14.0, 14.1, 16.0, 16.1

  **ReplaceMime**                            0x09       14.0, 14.1, 16.0, 16.1

  **Source**                                 0x0B       14.0, 14.1, 16.0, 16.1

  **FolderId**                               0x0C       14.0, 14.1, 16.0, 16.1

  **ItemId**                                 0x0D       14.0, 14.1, 16.0, 16.1

  **LongId**                                 0x0E       14.0, 14.1, 16.0, 16.1

  **InstanceId**                             0x0F       14.0, 14.1, 16.0, 16.1

  **Mime**                                   0x10       14.0, 14.1, 16.0, 16.1

  **ClientId**                               0x11       14.0, 14.1, 16.0, 16.1

  **Status**                                 0x12       14.0, 14.1, 16.0, 16.1

  **AccountId**                              0x13       14.1, 16.0, 16.1

  **Forwardees**                             0x15       16.0, 16.1

  **Forwardee**                              0x16       16.0, 16.1

  **Name**                                   0x17       16.0, 16.1

  **Email**                                  0x18       16.0, 16.1
  ------------------------------------------------------------------------------

##### Code Page 22: Email2

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 22 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  ------------------------------------------------------------------------------
  Tag name                                   Token      Protocol versions
  ------------------------------------------ ---------- ------------------------
  **UmCallerID**                             0x05       14.0, 14.1, 16.0, 16.1

  **UmUserNotes**                            0x06       14.0, 14.1, 16.0, 16.1

  **UmAttDuration**                          0x07       14.0, 14.1, 16.0, 16.1

  **UmAttOrder**                             0x08       14.0, 14.1, 16.0, 16.1

  **ConversationId**                         0x09       14.0, 14.1, 16.0, 16.1

  **ConversationIndex**                      0x0A       14.0, 14.1, 16.0, 16.1

  **LastVerbExecuted**                       0x0B       14.0, 14.1, 16.0, 16.1

  **LastVerbExecutionTime**                  0x0C       14.0, 14.1, 16.0, 16.1

  **ReceivedAsBcc**                          0x0D       14.0, 14.1, 16.0, 16.1

  **Sender**                                 0x0E       14.0, 14.1, 16.0, 16.1

  **CalendarType**                           0x0F       14.0, 14.1, 16.0, 16.1

  **IsLeapMonth**                            0x10       14.0, 14.1, 16.0, 16.1

  **AccountId**                              0x11       14.1, 16.0, 16.1

  **FirstDayOfWeek**                         0x12       14.1, 16.0, 16.1

  **MeetingMessageType**                     0x13       14.1, 16.0, 16.1

  **IsDraft**                                0x15       16.0, 16.1

  **Bcc**                                    0x16       16.0, 16.1

  **Send**                                   0x17       16.0, 16.1
  ------------------------------------------------------------------------------

##### Code Page 23: Notes

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 23 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  ------------------------------------------------------------------------------
  Tag name                                   Token      Protocol versions
  ------------------------------------------ ---------- ------------------------
  **Subject**                                0x05       14.0, 14.1, 16.0, 16.1

  **MessageClass**                           0x06       14.0, 14.1, 16.0, 16.1

  **LastModifiedDate**                       0x07       14.0, 14.1, 16.0, 16.1

  **Categories**                             0x08       14.0, 14.1, 16.0, 16.1

  **Category**                               0x09       14.0, 14.1, 16.0, 16.1
  ------------------------------------------------------------------------------

##### Code Page 24: RightsManagement

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 24 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  -------------------------------------------------------------------------
  Tag name                                   Token      Protocol versions
  ------------------------------------------ ---------- -------------------
  **RightsManagementSupport**                0x05       14.1, 16.0, 16.1

  **RightsManagementTemplates**              0x06       14.1, 16.0, 16.1

  **RightsManagementTemplate**               0x07       14.1, 16.0, 16.1

  **RightsManagementLicense**                0x08       14.1, 16.0, 16.1

  **EditAllowed**                            0x09       14.1, 16.0, 16.1

  **ReplyAllowed**                           0x0A       14.1, 16.0, 16.1

  **ReplyAllAllowed**                        0x0B       14.1, 16.0, 16.1

  **ForwardAllowed**                         0x0C       14.1, 16.0, 16.1

  **ModifyRecipientsAllowed**                0x0D       14.1, 16.0, 16.1

  **ExtractAllowed**                         0x0E       14.1, 16.0, 16.1

  **PrintAllowed**                           0x0F       14.1, 16.0, 16.1

  **ExportAllowed**                          0x10       14.1, 16.0, 16.1

  **ProgrammaticAccessAllowed**              0x11       14.1, 16.0, 16.1

  **Owner**                                  0x12       14.1, 16.0, 16.1

  **ContentExpiryDate**                      0x13       14.1, 16.0, 16.1

  **TemplateID**                             0x14       14.1, 16.0, 16.1

  **TemplateName**                           0x15       14.1, 16.0, 16.1

  **TemplateDescription**                    0x16       14.1, 16.0, 16.1

  **ContentOwner**                           0x17       14.1, 16.0, 16.1

  **RemoveRightsManagementProtection**       0x18       14.1, 16.0, 16.1
  -------------------------------------------------------------------------

##### Code Page 25: Find

The tags and tokens in [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 25 are listed in the following table. The ActiveSync protocol versions that support each tag/token are specified in the \"Protocol versions\" column of the table. For details about the ActiveSync protocol versions that can be used by a client or a server, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d).

  -----------------------------------------------------------------------
  Tag name                              Token       Protocol versions
  ------------------------------------- ----------- ---------------------
  **Find**                              0x05        16.1

  **SearchId**                          0x06        16.1

  **ExecuteSearch**                     0x07        16.1

  **MailBoxSearchCriterion**            0x08        16.1

  **Query**                             0x09        16.1

  **Status**                            0x0A        16.1

  **FreeText**                          0x0B        16.1

  **Options**                           0x0C        16.1

  **Range**                             0x0D        16.1

  **DeepTraversal**                     0x0E        16.1

  **Response**                          0x11        16.1

  **Result**                            0x12        16.1

  **Properties**                        0x13        16.1

  **Preview**                           0x14        16.1

  **HasAttachments**                    0x15        16.1

  **Total**                             0x16        16.1

  **DisplayCc**                         0x17        16.1

  **DisplayBcc**                        0x18        16.1

  **GalSearchCriterion**                0x19        16.1

  **MaxPictures**                       0x20        16.1

  **MaxSize**                           0x21        16.1

  **Picture**                           0x22        16.1
  -----------------------------------------------------------------------

### Processing Rules

The processing rules for this algorithm are specified in [\[WBXML1.2\]](https://go.microsoft.com/fwlink/?LinkId=160492).

This algorithm uses the following features that are specified in \[WBXML1.2\]:

-   [**WBXML tokens**](#gt_ec12fbbc-e750-435b-bc6e-919038ba9127) to encode [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) tags

-   [**WBXML code pages**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) to support multiple [**XML namespaces**](#gt_485f05b3-df3b-45ac-b8bf-d05f5d185a24)

-   Inline strings

-   Opaque data

This algorithm does not use the following features that are specified in \[WBXML1.2\]:

-   String tables

-   Entities

-   Processing instructions

-   Attribute encoding

# Algorithm Examples

The following example shows the [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc) encoding of a server response that contains a new [**contact**](#gt_48d3e923-3081-4b1c-a8b4-db07cc022128) and provides a byte-by-byte description of the encoding.

The following [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) represents a server response to a request to create a new contact.

1.  \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

    \<Sync xmlns=\"AirSync\" xmlns:airsyncbase=\"AirSyncBase\" xmlns:contacts=\"Contacts\"\>

    \<Collections\>

    \<Collection\>

    \<Class\>Contacts\</Class\>

    \<SyncKey\>2\</SyncKey\>

    \<CollectionId\>2\</CollectionId\>

    \<Status\>1\</Status\>

    \<Commands\>

    \<Add\>

    \<ServerId\>2:1\</ServerId\>

    \<ApplicationData\>

    \<airsyncbase:Body\>

    \<airsyncbase:Type\>1\</airsyncbase:Type\>

    \<airsyncbase:EstimatedDataSize\>0\</airsyncbase:EstimatedDataSize\>

    \<airsyncbase:Truncated\>1\</airsyncbase:Truncated\>

    \</airsyncbase:Body\>

    \<contacts:FileAs\>Funk, Don\</contacts:FileAs\>

    \<contacts:FirstName\>Don\</contacts:FirstName\>

    \<contacts:LastName\>Funk\</contacts:LastName\>

    \<airsyncbase:NativeBodyType\>1\</airsyncbase:NativeBodyType\>

    \</ApplicationData\>

    \</Add\>

    \</Commands\>

    \</Collection\>

    \</Collections\>

    \</Sync\>

The Exchange ActiveSync: WAP Binary XML (WBXML) Algorithm encodes this XML as the following WBXML encoding.

28. 03 01 6A 00 45 5C 4F 50 03 43 6F 6E 74 61 63 74 73 00 01 4B 03 32 00 01 52 03 32 00 01 4E 03 31 00 01 56 47 4D 03 32 3A 31 00 01 5D 00 11 4A 46 03 31 00 01 4C 03 30 00 01 4D 03 31 00 01 01 00 01 5E 03 46 75 6E 6B 2C 20 44 6F 6E 00 01 5F 03 44 6F 6E 00 01 69 03 46 75 6E 6B 00 01 00 11 56 03 31 00 01 01 01 01 01 01 01

The following table explains each byte in the WBXML encoding.

  -------------------------------------------------------------------------------------------------------------------------
  Bytes                           Description
  ------------------------------- -----------------------------------------------------------------------------------------
  03                              Version number -- WBXML version 1.3

  01                              Unknown public identifier

  6A                              Charset = UTF-8

  00                              String table length

  45                              \<airsync:Sync\>, with content (0x05 + 0x40)

  5C                              \<airsync:Collections\>, with content

  4F                              \<airsync:Collection\>, with content

  50                              \<airsync:Class\>, with content

  03                              Inline string follows

  43 6F 6E 74 61 63 74 73 00      \"contacts\" (the 00 is the end of the string)

  01                              \</airsync:Class\>

  4B                              \<airsync:SyncKey\>, with content

  03                              Inline string follows

  32 00                           \"2\"

  01                              \</airsync:SyncKey\>

  52                              \<airsync:CollectionID\>, with content

  03                              Inline string follows

  32 00                           \"2\"

  01                              \</airsync:CollectionID\>

  4E                              \<airsync:Status\>, with content

  03                              Inline string follows

  31 00                           \"1\"

  01                              \</airsync:Status\>

  56                              \<airsync:Commands\>, with content

  47                              \<airsync:Add\>, with content

  4D                              \<airsync:ServerId\>, with content

  03                              Inline string follows

  32 3A 31 00                     \"2:1\"

  01                              \</airsync:ServerId\>

  5D                              \<airsync:ApplicationData\>, with content

  00 11                           Select [**WBXML code page**](#gt_e4c66ae5-4c5e-4481-9c78-190762a37db1) 17 (AirSyncBase)

  4A                              \<airsyncbase:Body\>, with content

  46                              \<airsyncbase:Type\>, with content

  03                              Inline string follows

  31 00                           \"1\"

  01                              \</airsyncbase:Type\>

  4C                              \<airsyncbase:EstimatedDataSize\>, with content

  03                              Inline string follows

  30 00                           \"0\"

  01                              \</airsyncbase:EstimatedDataSize\>

  4D                              \<airsyncbase:Truncated\>, with content

  03                              Inline string follows

  31 00                           \"1\"

  01                              \</airsyncbase:Truncated\>

  01                              \</airsyncbase:Body\>

  00 01                           Select WBXML code page 1 (Contacts)

  5E                              \<contacts:FileAs\>, with content

  03                              Inline string follows

  46 75 6E 6B 2C 20 44 6F 6E 00   \"Funk, Don\"

  01                              \</contacts:FileAs\>

  5F                              \<contacts:FirstName\>, with content

  03                              Inline string follows

  44 6F 6E 00                     \"Don\"

  01                              \</contacts:FirstName\>

  69                              \<contacts:LastName\>, with content

  03                              Inline string follows

  46 75 6E 6B 00                  \"Funk\"

  01                              \</contacts:LastName\>

  00 11                           Select WBXML code page 17 (AirSyncBase)

  56                              \<airsyncbase:NativeBodyType\>, with content

  03                              Inline string follows

  31 00                           \"1\"

  01                              \</airsyncbase:NativeBodyType\>

  01                              \</airsync:ApplicationData\>

  01                              \</airsync:Add\>

  01                              \</airsync:Commands\>

  01                              \</airsync:Collection\>

  01                              \</airsync:Collections\>

  01                              \</airsync:Sync\>
  -------------------------------------------------------------------------------------------------------------------------

# Security

## Security Considerations for Implementers

In most cases, all communication between the client and server happens across an [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) connection secured by the [**Secure Sockets Layer (SSL)**](#gt_d7ef66a9-f154-4d88-bda9-98bdf7235352) protocol. The SSL connection is assumed to be secure enough to transmit confidential data, such as user credentials and sensitive e-mail. The SSL certificate on the server is assumed to be trusted by the client application.

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

-   Windows Communication Apps

-   Windows 10 operating system

-   Windows Server 2016 operating system

-   Windows 11 operating system

-   Microsoft Exchange Server Subscription Edition

Exceptions, if any, are noted in this section. If an update version, service pack or Knowledge Base (KB) number appears with a product name, the behavior changed in that update. The new behavior also applies to subsequent updates unless otherwise specified. If a product edition appears with the product version, behavior is different in that product edition.

Unless otherwise specified, any statement of optional behavior in this specification that is prescribed using the terms \"SHOULD\" or \"SHOULD NOT\" implies product behavior in accordance with the SHOULD or SHOULD NOT prescription. Unless otherwise specified, the term \"MAY\" implies that the product does not follow the prescription.

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
  [5](#Section_68b22e0e6b9f48b2819cd2e6c7462f5c) Appendix A: Product Behavior   Updated list of supported products.   Major

  ------------------------------------------------------------------------------------------------------------------------------------

# Index

A

[Abstract data model](#abstract-data-model) 9

ActiveSync WBXML

[overview](#activesync-wbxml-algorithm-details) 9

[Applicability](#applicability-statement) 7

C

[Change tracking](#change-tracking) 42

[Code pages](#code-pages) 9

D

[Data model - abstract](#abstract-data-model) 9

E

Examples

[overview](#algorithm-examples) 37

G

[Glossary](#glossary) 5

I

[Implementer - security considerations](#security-considerations-for-implementers) 40

[Index of security parameters](#index-of-security-parameters) 40

[Informative references](#informative-references) 6

[Initialization](#initialization) 9

[Introduction](#introduction) 5

N

[Normative references](#normative-references) 6

O

[Overview (synopsis)](#overview) 6

P

[Parameters - security index](#index-of-security-parameters) 40

[Processing rules](#processing-rules) 36

[Product behavior](#appendix-a-product-behavior) 41

R

References

[informative](#informative-references) 6

[normative](#normative-references) 6

Relationship to

[other algorithms](#relationship-to-protocols-and-other-algorithms) 6

[other protocols](#relationship-to-protocols-and-other-algorithms) 6

S

Security

[implementer considerations](#security-considerations-for-implementers) 40

[parameter index](#index-of-security-parameters) 40

[Standards assignments](#standards-assignments) 8

T

[Tracking changes](#change-tracking) 42

W

WBXML

[code pages](#code-pages) 9
