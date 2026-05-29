**\[MS-ASCNTC\]:**

**Exchange ActiveSync: Contact Class Protocol**

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

  11/3/2010    7.1                Minor            Clarified the meaning of the technical content.

  3/18/2011    7.2                Minor            Clarified the meaning of the technical content.

  8/5/2011     8.0                Major            Significantly changed the technical content.

  10/7/2011    8.1                Minor            Clarified the meaning of the technical content.

  1/20/2012    9.0                Major            Significantly changed the technical content.

  4/27/2012    9.0                None             No changes to the meaning, language, or formatting of the technical content.

  7/16/2012    10.0               Major            Significantly changed the technical content.

  10/8/2012    10.1               Minor            Clarified the meaning of the technical content.

  2/11/2013    10.1               None             No changes to the meaning, language, or formatting of the technical content.

  7/26/2013    11.0               Major            Significantly changed the technical content.

  11/18/2013   11.0               None             No changes to the meaning, language, or formatting of the technical content.

  2/10/2014    11.0               None             No changes to the meaning, language, or formatting of the technical content.

  4/30/2014    12.0               Major            Significantly changed the technical content.

  7/31/2014    12.0               None             No changes to the meaning, language, or formatting of the technical content.

  10/30/2014   12.1               Minor            Clarified the meaning of the technical content.

  5/26/2015    13.0               Major            Significantly changed the technical content.

  6/30/2015    14.0               Major            Significantly changed the technical content.

  9/14/2015    15.0               Major            Significantly changed the technical content.

  6/9/2016     16.0               Major            Significantly changed the technical content.

  2/28/2017    17.0               Major            Significantly changed the technical content.

  4/18/2017    17.0               None             No changes to the meaning, language, or formatting of the technical content.

  7/24/2018    18.0               Major            Significantly changed the technical content.

  10/1/2018    19.0               Major            Significantly changed the technical content.

  12/11/2018   19.1               Minor            Clarified the meaning of the technical content.

  4/29/2022    20.0               Major            Significantly changed the technical content.

  5/20/2025    21.0               Major            Significantly changed the technical content.
  -------------------------------------------------------------------------------------------------------------------------------

Table of Contents

[1 Introduction [7](#introduction)](#introduction)

[1.1 Glossary [7](#glossary)](#glossary)

[1.2 References [8](#references)](#references)

[1.2.1 Normative References [8](#normative-references)](#normative-references)

[1.2.2 Informative References [8](#informative-references)](#informative-references)

[1.3 Overview [8](#overview)](#overview)

[1.4 Relationship to Other Protocols [9](#relationship-to-other-protocols)](#relationship-to-other-protocols)

[1.5 Prerequisites/Preconditions [9](#prerequisitespreconditions)](#prerequisitespreconditions)

[1.6 Applicability Statement [9](#applicability-statement)](#applicability-statement)

[1.7 Versioning and Capability Negotiation [9](#versioning-and-capability-negotiation)](#versioning-and-capability-negotiation)

[1.8 Vendor-Extensible Fields [9](#vendor-extensible-fields)](#vendor-extensible-fields)

[1.9 Standards Assignments [9](#standards-assignments)](#standards-assignments)

[2 Messages [10](#messages)](#messages)

[2.1 Transport [10](#transport)](#transport)

[2.2 Message Syntax [10](#message-syntax)](#message-syntax)

[2.2.1 Namespaces [10](#namespaces)](#namespaces)

[2.2.2 Elements [10](#elements)](#elements)

[2.2.2.1 AccountName [13](#accountname)](#accountname)

[2.2.2.2 Alias [14](#alias)](#alias)

[2.2.2.3 Anniversary [14](#anniversary)](#anniversary)

[2.2.2.4 AssistantName [15](#assistantname)](#assistantname)

[2.2.2.5 AssistantPhoneNumber [16](#assistantphonenumber)](#assistantphonenumber)

[2.2.2.6 Birthday [16](#birthday)](#birthday)

[2.2.2.7 Body [17](#body)](#body)

[2.2.2.7.1 Body (AirSyncBase Namespace) [17](#body-airsyncbase-namespace)](#body-airsyncbase-namespace)

[2.2.2.7.2 Body (Contacts Namespace) [17](#body-contacts-namespace)](#body-contacts-namespace)

[2.2.2.8 BodySize [18](#bodysize)](#bodysize)

[2.2.2.9 BodyTruncated [19](#bodytruncated)](#bodytruncated)

[2.2.2.10 BusinessAddressCity [20](#businessaddresscity)](#businessaddresscity)

[2.2.2.11 BusinessAddressCountry [20](#businessaddresscountry)](#businessaddresscountry)

[2.2.2.12 BusinessAddressPostalCode [21](#businessaddresspostalcode)](#businessaddresspostalcode)

[2.2.2.13 BusinessAddressState [21](#businessaddressstate)](#businessaddressstate)

[2.2.2.14 BusinessAddressStreet [22](#businessaddressstreet)](#businessaddressstreet)

[2.2.2.15 BusinessFaxNumber [23](#businessfaxnumber)](#businessfaxnumber)

[2.2.2.16 BusinessPhoneNumber [23](#businessphonenumber)](#businessphonenumber)

[2.2.2.17 Business2PhoneNumber [24](#business2phonenumber)](#business2phonenumber)

[2.2.2.18 CarPhoneNumber [24](#carphonenumber)](#carphonenumber)

[2.2.2.19 Categories [25](#categories)](#categories)

[2.2.2.20 Category [26](#category)](#category)

[2.2.2.21 Children [26](#children)](#children)

[2.2.2.22 Child [27](#child)](#child)

[2.2.2.23 CompanyMainPhone [27](#companymainphone)](#companymainphone)

[2.2.2.24 CompanyName [28](#companyname)](#companyname)

[2.2.2.25 CustomerId [29](#customerid)](#customerid)

[2.2.2.26 Department [29](#department)](#department)

[2.2.2.27 Email1Address [30](#email1address)](#email1address)

[2.2.2.28 Email2Address [31](#email2address)](#email2address)

[2.2.2.29 Email3Address [31](#email3address)](#email3address)

[2.2.2.30 FileAs [32](#fileas)](#fileas)

[2.2.2.31 FirstName [33](#firstname)](#firstname)

[2.2.2.32 GovernmentId [33](#governmentid)](#governmentid)

[2.2.2.33 HomeAddressCity [34](#homeaddresscity)](#homeaddresscity)

[2.2.2.34 HomeAddressCountry [34](#homeaddresscountry)](#homeaddresscountry)

[2.2.2.35 HomeAddressPostalCode [35](#homeaddresspostalcode)](#homeaddresspostalcode)

[2.2.2.36 HomeAddressState [36](#homeaddressstate)](#homeaddressstate)

[2.2.2.37 HomeAddressStreet [36](#homeaddressstreet)](#homeaddressstreet)

[2.2.2.38 HomeFaxNumber [37](#homefaxnumber)](#homefaxnumber)

[2.2.2.39 HomePhoneNumber [37](#homephonenumber)](#homephonenumber)

[2.2.2.40 Home2PhoneNumber [38](#home2phonenumber)](#home2phonenumber)

[2.2.2.41 IMAddress [39](#imaddress)](#imaddress)

[2.2.2.42 IMAddress2 [39](#imaddress2)](#imaddress2)

[2.2.2.43 IMAddress3 [40](#imaddress3)](#imaddress3)

[2.2.2.44 JobTitle [40](#jobtitle)](#jobtitle)

[2.2.2.45 LastName [41](#lastname)](#lastname)

[2.2.2.46 ManagerName [42](#managername)](#managername)

[2.2.2.47 MiddleName [42](#middlename)](#middlename)

[2.2.2.48 MMS [43](#mms)](#mms)

[2.2.2.49 MobilePhoneNumber [43](#mobilephonenumber)](#mobilephonenumber)

[2.2.2.50 NickName [44](#nickname)](#nickname)

[2.2.2.51 OfficeLocation [45](#officelocation)](#officelocation)

[2.2.2.52 OtherAddressCity [45](#otheraddresscity)](#otheraddresscity)

[2.2.2.53 OtherAddressCountry [46](#otheraddresscountry)](#otheraddresscountry)

[2.2.2.54 OtherAddressPostalCode [46](#otheraddresspostalcode)](#otheraddresspostalcode)

[2.2.2.55 OtherAddressState [47](#otheraddressstate)](#otheraddressstate)

[2.2.2.56 OtherAddressStreet [48](#otheraddressstreet)](#otheraddressstreet)

[2.2.2.57 PagerNumber [48](#pagernumber)](#pagernumber)

[2.2.2.58 Picture [49](#picture)](#picture)

[2.2.2.59 RadioPhoneNumber [49](#radiophonenumber)](#radiophonenumber)

[2.2.2.60 Spouse [50](#spouse)](#spouse)

[2.2.2.61 Suffix [51](#suffix)](#suffix)

[2.2.2.62 Title [51](#title)](#title)

[2.2.2.63 WebPage [52](#webpage)](#webpage)

[2.2.2.64 WeightedRank [52](#weightedrank)](#weightedrank)

[2.2.2.65 YomiCompanyName [53](#yomicompanyname)](#yomicompanyname)

[2.2.2.66 YomiFirstName [54](#yomifirstname)](#yomifirstname)

[2.2.2.67 YomiLastName [54](#yomilastname)](#yomilastname)

[3 Protocol Details [56](#protocol-details)](#protocol-details)

[3.1 Client Details [56](#client-details)](#client-details)

[3.1.1 Abstract Data Model [56](#abstract-data-model)](#abstract-data-model)

[3.1.2 Timers [56](#timers)](#timers)

[3.1.3 Initialization [56](#initialization)](#initialization)

[3.1.4 Higher-Layer Triggered Events [56](#higher-layer-triggered-events)](#higher-layer-triggered-events)

[3.1.4.1 Synchronizing Contact Data Between Client and Server [56](#synchronizing-contact-data-between-client-and-server)](#synchronizing-contact-data-between-client-and-server)

[3.1.4.2 Searching a Server for Contact Data [56](#searching-a-server-for-contact-data)](#searching-a-server-for-contact-data)

[3.1.4.3 Requesting Details for Specific Contacts [56](#requesting-details-for-specific-contacts)](#requesting-details-for-specific-contacts)

[3.1.4.4 Refreshing the Recipient Information Cache [56](#refreshing-the-recipient-information-cache)](#refreshing-the-recipient-information-cache)

[3.1.5 Message Processing Events and Sequencing Rules [57](#message-processing-events-and-sequencing-rules)](#message-processing-events-and-sequencing-rules)

[3.1.5.1 ItemOperations Command Request [57](#itemoperations-command-request)](#itemoperations-command-request)

[3.1.5.2 Search Command Request [57](#search-command-request)](#search-command-request)

[3.1.5.3 Sync Command Request [57](#sync-command-request)](#sync-command-request)

[3.1.5.3.1 Omitting Ghosted Properties from a Sync Change Request [57](#omitting-ghosted-properties-from-a-sync-change-request)](#omitting-ghosted-properties-from-a-sync-change-request)

[3.1.5.4 Truncating the Contact Notes Field [58](#truncating-the-contact-notes-field)](#truncating-the-contact-notes-field)

[3.1.6 Timer Events [58](#timer-events)](#timer-events)

[3.1.7 Other Local Events [58](#other-local-events)](#other-local-events)

[3.2 Server Details [58](#server-details)](#server-details)

[3.2.1 Abstract Data Model [58](#abstract-data-model-1)](#abstract-data-model-1)

[3.2.2 Timers [58](#timers-1)](#timers-1)

[3.2.3 Initialization [59](#initialization-1)](#initialization-1)

[3.2.4 Higher-Layer Triggered Events [59](#higher-layer-triggered-events-1)](#higher-layer-triggered-events-1)

[3.2.4.1 Synchronizing Contact Data Between Client and Server [59](#synchronizing-contact-data-between-client-and-server-1)](#synchronizing-contact-data-between-client-and-server-1)

[3.2.4.2 Searching for Contact Data [59](#searching-for-contact-data)](#searching-for-contact-data)

[3.2.4.3 Retrieving Details for Specific Contacts [59](#retrieving-details-for-specific-contacts)](#retrieving-details-for-specific-contacts)

[3.2.4.4 Refreshing the Recipient Information Cache [59](#refreshing-the-recipient-information-cache-1)](#refreshing-the-recipient-information-cache-1)

[3.2.5 Message Processing Events and Sequencing Rules [59](#message-processing-events-and-sequencing-rules-1)](#message-processing-events-and-sequencing-rules-1)

[3.2.5.1 ItemOperations Command Response [59](#itemoperations-command-response)](#itemoperations-command-response)

[3.2.5.2 Search Command Response [60](#search-command-response)](#search-command-response)

[3.2.5.3 Sync Command Response [60](#sync-command-response)](#sync-command-response)

[3.2.5.3.1 Omitting Ghosted Properties from a Sync Change Request [60](#omitting-ghosted-properties-from-a-sync-change-request-1)](#omitting-ghosted-properties-from-a-sync-change-request-1)

[3.2.6 Timer Events [60](#timer-events-1)](#timer-events-1)

[3.2.7 Other Local Events [60](#other-local-events-1)](#other-local-events-1)

[4 Protocol Examples [61](#protocol-examples)](#protocol-examples)

[5 Security [63](#security)](#security)

[5.1 Security Considerations for Implementers [63](#security-considerations-for-implementers)](#security-considerations-for-implementers)

[5.2 Index of Security Parameters [63](#index-of-security-parameters)](#index-of-security-parameters)

[6 Appendix A: Full XML Schema [64](#appendix-a-full-xml-schema)](#appendix-a-full-xml-schema)

[6.1 Contacts Namespace Schema [64](#contacts-namespace-schema)](#contacts-namespace-schema)

[6.2 Contacts2 Namespace Schema [68](#contacts2-namespace-schema)](#contacts2-namespace-schema)

[7 Appendix B: Product Behavior [70](#appendix-b-product-behavior)](#appendix-b-product-behavior)

[8 Change Tracking [71](#change-tracking)](#change-tracking)

[9 Index [72](#index)](#index)

# Introduction

The Exchange ActiveSync: Contact Class Protocol enables the communication of [**contact (2)**](#gt_48d3e923-3081-4b1c-a8b4-db07cc022128) data between a mobile device and the server in the ActiveSync protocol.

Sections 1.5, 1.8, 1.9, 2, and 3 of this specification are normative. All other sections and examples in this specification are informative.

## Glossary

This document uses the following terms:

> []{#gt_d046b6e2-3f79-47e1-87d7-754566744dcd .anchor}**alias**: An alternate name that can be used to reference an object or element.
>
> []{#gt_179b9392-9019-45a3-880b-26f6890522b7 .anchor}**base64 encoding**: A binary-to-text encoding scheme whereby an arbitrary sequence of bytes is converted to a sequence of printable ASCII characters, as described in [\[RFC4648\]](https://go.microsoft.com/fwlink/?LinkId=90487).
>
> []{#gt_48d3e923-3081-4b1c-a8b4-db07cc022128 .anchor}**contact**: (1) A presence entity (presentity) whose presence information can be tracked.
>
> \(2\) An object of the contact class that represents a company or person whom a user can contact.
>
> []{#gt_b35ba7ae-4348-4a65-9d02-dabca97ccdec .anchor}**Contacts folder**: A [**Folder object**](#gt_0682daa7-c1b8-419b-8a32-6048833d0b72) that contains Contact objects.
>
> []{#gt_f2369991-a884-4843-a8fa-1505b6d5ece7 .anchor}**Coordinated Universal Time (UTC)**: A high-precision atomic time standard that approximately tracks Universal Time (UT). It is the basis for legal, civil time all over the Earth. Time zones around the world are expressed as positive and negative offsets from UTC. In this role, it is also referred to as Zulu time (Z) and Greenwich Mean Time (GMT). In these specifications, all references to UTC refer to the time at UTC-0 (or GMT).
>
> []{#gt_1175dd11-9368-41d5-98ed-d585f268ad4b .anchor}**distinguished name (DN)**: A name that uniquely identifies an object by using the relative distinguished name (RDN) for the object, and the names of container objects and domains that contain the object. The distinguished name (DN) identifies the object and its location in a tree.
>
> []{#gt_0682daa7-c1b8-419b-8a32-6048833d0b72 .anchor}**Folder object**: A messaging construct that is typically used to organize data into a hierarchy of objects containing Message objects and folder associated information (FAI) Message objects.
>
> []{#gt_c2354a51-451b-4296-88cd-3321c437d2c5 .anchor}**ghosted**: A property that is not deleted by the server if the element is not included in a Sync \<Change\> request message.
>
> []{#gt_4bcda02a-9b58-4abe-8c7d-d8336a130346 .anchor}**recipient information cache**: An information store that contains a list of the contacts with whom a user has interacted most often and most recently, and with whom the user is likely to interact again.
>
> []{#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc .anchor}**Wireless Application Protocol (WAP) Binary XML (WBXML)**: A compact binary representation of [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) that is designed to reduce the transmission size of XML documents over narrowband communication channels.
>
> []{#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85 .anchor}**XML**: The Extensible Markup Language, as described in [\[XML1.0\]](https://go.microsoft.com/fwlink/?LinkId=90599).
>
> []{#gt_a364f92c-0374-4568-b7f8-40bd74437dd5 .anchor}**XML element**: An [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) structure that typically consists of a start tag, an end tag, and the information between those tags. Elements can have attributes and can contain other elements.
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

\[MS-ASAIRS\] Microsoft Corporation, \"[Exchange ActiveSync: AirSyncBase Namespace Protocol](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c)\".

\[MS-ASCMD\] Microsoft Corporation, \"[Exchange ActiveSync: Command Reference Protocol](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a)\".

\[MS-ASDTYPE\] Microsoft Corporation, \"[Exchange ActiveSync: Data Types](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3)\".

\[MS-ASHTTP\] Microsoft Corporation, \"[Exchange ActiveSync: HTTP Protocol](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d)\".

\[MS-ASWBXML\] Microsoft Corporation, \"[Exchange ActiveSync: WAP Binary XML (WBXML) Algorithm](%5bMS-ASWBXML%5d.pdf#Section_39973eb11e404eb5ac7442781c5a33bc)\".

\[RFC2119\] Bradner, S., \"Key words for use in RFCs to Indicate Requirement Levels\", BCP 14, RFC 2119, March 1997, [https://www.rfc-editor.org/info/rfc2119](https://go.microsoft.com/fwlink/?LinkId=90317)

\[XMLNS\] Bray, T., Hollander, D., Layman, A., et al., Eds., \"Namespaces in XML 1.0 (Third Edition)\", W3C Recommendation, December 2009, [https://www.w3.org/TR/2009/REC-xml-names-20091208/](https://go.microsoft.com/fwlink/?LinkId=191840)

\[XMLSCHEMA1/2\] Thompson, H., Beech, D., Maloney, M., and Mendelsohn, N., Eds., \"XML Schema Part 1: Structures Second Edition\", W3C Recommendation, October 2004, [https://www.w3.org/TR/2004/REC-xmlschema-1-20041028/](https://go.microsoft.com/fwlink/?LinkId=90607)

\[XML\] World Wide Web Consortium, \"Extensible Markup Language (XML) 1.0 (Fourth Edition)\", W3C Recommendation 16 August 2006, edited in place 29 September 2006, [http://www.w3.org/TR/2006/REC-xml-20060816/](https://go.microsoft.com/fwlink/?LinkId=90598)

### Informative References

\[MS-OXPROTO\] Microsoft Corporation, \"[Exchange Server Protocols System Overview](%5bMS-OXPROTO%5d.pdf#Section_734ab967e43e425babe1974af56c0283)\".

## Overview

This protocol describes an [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) representation of [**contacts (2)**](#gt_48d3e923-3081-4b1c-a8b4-db07cc022128) that are used for client and server communication as described in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a). The contact data is included in protocol command requests when contact data is sent from the client to the server, and is included in protocol command responses when contact data is returned from the server to the client.

## Relationship to Other Protocols

This protocol describes the [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) representation of [**contacts (2)**](#gt_48d3e923-3081-4b1c-a8b4-db07cc022128) that are used by the command requests and command responses that are described in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a). The protocol governing the transmission of these commands between the client and the server is described in \[MS-ASCMD\]. The [**Wireless Application Protocol (WAP) Binary XML (WBXML)**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc), as described in [\[MS-ASWBXML\]](%5bMS-ASWBXML%5d.pdf#Section_39973eb11e404eb5ac7442781c5a33bc), is used to transmit the XML markup that constitutes the request body and the response body.

Some elements in the **Contact** class support being [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). The use of ghosted properties is described in \[MS-ASCMD\] section 2.2.3.179.

All data types in this document conform to the data type definitions that are described in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3). Common [**XML schema**](#gt_bd0ce6f9-c350-4900-827e-951265294067) elements used by other classes are defined in [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c).

For conceptual background information and overviews of the relationships and interactions between this and other protocols, see [\[MS-OXPROTO\]](%5bMS-OXPROTO%5d.pdf#Section_734ab967e43e425babe1974af56c0283).

## Prerequisites/Preconditions

None.

## Applicability Statement

This protocol describes a set of [**XML elements**](#gt_a364f92c-0374-4568-b7f8-40bd74437dd5) that are used to communicate [**contact (2)**](#gt_48d3e923-3081-4b1c-a8b4-db07cc022128) data when using the commands described in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a). This set of elements is applicable when communicating contact information between a mobile device and a server. These elements are not applicable when communicating other types of information that are supported by the ActiveSync protocol.

## Versioning and Capability Negotiation

None.

## Vendor-Extensible Fields

None.

## Standards Assignments

None.

# Messages

## Transport

This protocol consists of a series of [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) elements that are embedded inside of a command request or command response, as specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

The XML markup that constitutes the request body or the response body that is transmitted between the client and the server uses [**Wireless Application Protocol (WAP) Binary XML (WBXML)**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc), as specified in [\[MS-ASWBXML\]](%5bMS-ASWBXML%5d.pdf#Section_39973eb11e404eb5ac7442781c5a33bc).

## Message Syntax

The [**XML schemas**](#gt_bd0ce6f9-c350-4900-827e-951265294067) for the **Contacts** and **Contacts2** namespaces are described in section [6](#Section_1f5d5f3b158345fc9edebbfe11d40e18).

The markup that is used by this protocol MUST be well-formed XML, as specified in [\[XML\]](https://go.microsoft.com/fwlink/?LinkId=90598).

### Namespaces

This specification defines and references various [**XML namespaces**](#gt_485f05b3-df3b-45ac-b8bf-d05f5d185a24) using the mechanisms specified in [\[XMLNS\]](https://go.microsoft.com/fwlink/?LinkId=191840). Although this specification associates a specific XML namespace prefix for each XML namespace that is used, the choice of any particular XML namespace prefix is implementation-specific and not significant for interoperability.

  ------------------------------------------------------------------------------------------------------------------------------------------------------
  Prefix           Namespace URI                          Reference
  ---------------- -------------------------------------- ----------------------------------------------------------------------------------------------
  airsyncbase      **AirSyncBase**                        [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c)

  contacts         **Contacts**                           

  contacts2        **Contacts2**                          

  airsync          **AirSync**                            [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21

  itemoperations   **ItemOperations**                     \[MS-ASCMD\] section 2.2.1.10

  search           **Search**                             \[MS-ASCMD\] section 2.2.1.16

  xs               **http://www.w3.org/2001/XMLSchema**   [\[XMLSCHEMA1/2\]](https://go.microsoft.com/fwlink/?LinkId=90607)
  ------------------------------------------------------------------------------------------------------------------------------------------------------

### Elements

Elements of the **Contact** class are defined in three namespaces: **Contacts**, **Contacts2**, and **AirSyncBase**. All **Contact** class elements are specified in this document; however, elements defined in the **AirSyncBase** namespace are further specified in [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c).

The elements are defined in the **Contacts** namespace, except where indicated by the presence of a namespace prefix, as defined in section [2.2.1](#Section_a46d14d251014c5aa46301e1854c0ae4). A prefix is used for an element in the **Contacts** namespace only where necessary to disambiguate the element from another one of the same name.

Except where otherwise specified in the following sections, each element of the **Contact** class is used in ActiveSync command requests and responses as follows:

-   As an optional child element of the **itemoperations:Schema** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.158) in **ItemOperations** command requests (\[MS-ASCMD\] section 2.2.1.10)

-   As an optional child element of the **itemoperations:Properties** element (\[MS-ASCMD\] section 2.2.3.139.2) in **ItemOperations** command responses (\[MS-ASCMD\] section 2.2.1.10)

-   As an optional child element of the **search:Properties** element (\[MS-ASCMD\] section 2.2.3.139.3) in **Search** command responses (\[MS-ASCMD\] section 2.2.1.16)

-   As an optional child element of the **airsync:ApplicationData** element (\[MS-ASCMD\] section 2.2.3.11) in **Sync** command requests (\[MS-ASCMD\] section 2.2.1.21)

-   As an optional child element of the **airsync:ApplicationData** element (\[MS-ASCMD\] section 2.2.3.11) in **Sync** command responses (\[MS-ASCMD\] section 2.2.1.21)

The following table summarizes the set of common [**XML schema**](#gt_bd0ce6f9-c350-4900-827e-951265294067) element definitions defined or used by this specification. XML schema element definitions that are specific to one or more particular operations are specified further in sections [3.1.5.1](#Section_3aa2b0d9ebec4da2b827050aedb7ed26), [3.1.5.2](#Section_8f7ee7b12c3c479aa7c5e3a89f7660c1), [3.1.5.3](#Section_1168e35fbec340769ce0aea2c355df30), [3.2.5.1](#Section_1ea6daff2361457dbe7efd3dd660cfc3), [3.2.5.2](#Section_64fafb34ca9a499cb258e43e6ef8f943), and [3.2.5.3](#Section_685a7252acb24f438c60f53db80b6d0c).

  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Element name                                                                                     Description
  ------------------------------------------------------------------------------------------------ -----------------------------------------------------------------------------------------------------------------------------------------
  **Anniversary** (section [2.2.2.3](#Section_7c5501719863429a9e8716f996d13bf2))                   The wedding anniversary date for the contact.

  **AssistantName** (section [2.2.2.4](#Section_bb4a95c796644a84b07d7b9ae1079aca))                 The name of the contact\'s assistant.

  **AssistantPhoneNumber** (section [2.2.2.5](#Section_9fad249dbf30495fba53462c755292dd))          The phone number of the contact\'s assistant.

  **Birthday** (section [2.2.2.6](#Section_c157c6058f4b421482135cd2b035d6b4))                      The birth date of the contact.

  **Business2PhoneNumber** (section [2.2.2.17](#Section_981d6597352446c79da67b0d6329b5bb))         The second business telephone number for the contact.

  **BusinessAddressCity** (section [2.2.2.10](#Section_ace4d5bd32a649f7826ec179262242a7))          The business city of the contact.

  **BusinessPhoneNumber** (section [2.2.2.16](#Section_b90f58a3689c407f834d5331d21566a5))          The business telephone number for the contact.

  **WebPage** (section [2.2.2.63](#Section_3136c56531b94bffb1b6633f104f4178))                      The Web site or personal Web page for the contact.

  **BusinessAddressCountry** (section [2.2.2.11](#Section_8b9cb2f489c040ce99a35ff487d353b2))       The business country/region for the contact.

  **Department** (section [2.2.2.26](#Section_732191b3c46644cf9bc163d90ec774f4))                   The department name for the contact.

  **Email1Address** (section [2.2.2.27](#Section_91bd445da02a4b99ab530208087e7ac8))                The first e-mail address for the contact.

  **Email2Address** (section [2.2.2.28](#Section_0f217e8118274ac1a7e4be3d8ad47ea3))                The second e-mail address for the contact.

  **Email3Address** (section [2.2.2.29](#Section_9aea588347a9410db6266f96d80a2e56))                The third e-mail address for the contact.

  **BusinessFaxNumber** (section [2.2.2.15](#Section_a5deed91b6634042a846db37976ea80e))            The business fax number for the contact.

  **FileAs** (section [2.2.2.30](#Section_f0384e7f7a974177b84e60dcdec15b78))                       The filing string for the contact.

  **Alias** (section [2.2.2.2](#Section_53e4357a0faf40698361912cfe9ddb39))                         The user\'s [**alias**](#gt_d046b6e2-3f79-47e1-87d7-754566744dcd).

  **WeightedRank** (section [2.2.2.64](#Section_bc96fc4515f84c42aff3400ddd953584))                 The rank this entry possesses in the [**recipient information cache**](#gt_4bcda02a-9b58-4abe-8c7d-d8336a130346).

  **FirstName** (section [2.2.2.31](#Section_718025f4609b4d2babf961253e88fad8))                    The contact\'s first name.

  **MiddleName** (section [2.2.2.47](#Section_7b2703e6c6df48ad8785be12d8923cc9))                   The contact\'s middle name.

  **HomeAddressCity** (section [2.2.2.33](#Section_67e8a284134b4dfdb29020f257d9117f))              The home city for the contact.

  **HomeAddressCountry** (section [2.2.2.34](#Section_47c3f870fb9e471a8bf9a4a5943fb30d))           The home country/region for the contact.

  **HomeFaxNumber** (section [2.2.2.38](#Section_3654c1ddb7ab437eaf4d74ce9cfb1fcd))                The home fax number for the contact.

  **HomePhoneNumber** (section [2.2.2.39](#Section_e907ae1c09e14314be2b7e8f6adf4dd5))              The home phone number for the contact.

  **Home2PhoneNumber** (section [2.2.2.40](#Section_42737372a3b64ee6a56fdfb0fdcfdfdf))             The second home phone number for the contact.

  **HomeAddressPostalCode** (section [2.2.2.35](#Section_d249041dc718431a98f2d14a69068b4f))        The home postal code for the contact.

  **HomeAddressState** (section [2.2.2.36](#Section_eb60868ee1a04a8081e12a21f5f240b3))             The home state for the contact.

  **HomeAddressStreet** (section [2.2.2.37](#Section_3ce906a052cc4ecdb8c04ac25d1596ef))            The home street address for the contact.

  **MobilePhoneNumber** (section [2.2.2.49](#Section_d0272e53dbd34a11b9d293cbedb0875c))            The mobile phone number for the contact.

  **Suffix** (section [2.2.2.61](#Section_abb4d0d22f6748aab4720a1b90494c85))                       The suffix for the contact\'s name.

  **CompanyName** (section [2.2.2.24](#Section_5171b15796084d07af93644b154fbd30))                  The company name for the contact.

  **OtherAddressCity** (section [2.2.2.52](#Section_3fcf5b54d9d84f3f843526b95f257681))             The city of the contact\'s alternative address.

  **OtherAddressCountry** (section [2.2.2.53](#Section_4a6c9046669d44a29eb705db8581ef85))          The country/region of the contact\'s alternative address.

  **CarPhoneNumber** (section [2.2.2.18](#Section_9cee613b8492416585766a1597c08084))               The car telephone number for the contact.

  **OtherAddressPostalCode** (section [2.2.2.54](#Section_cffff1d88f154b80a970a4b62370a373))       The postal code of the contact\'s alternative address.

  **OtherAddressState** (section [2.2.2.55](#Section_806c612555274927a74c406f7892294c))            The state of the contact\'s alternative address.

  **OtherAddressStreet** (section [2.2.2.56](#Section_b6e150e240ab4949b05e29988f8b0773))           The street address of the contact\'s alternative address.

  **PagerNumber** (section [2.2.2.57](#Section_0b3c738c66be44228c9684f180f65a48))                  The pager number for the contact.

  **Title** (section [2.2.2.62](#Section_cc979d4b7117442cb007cb4c7a72fad1))                        The contact\'s business title.

  **BusinessAddressPostalCode** (section [2.2.2.12](#Section_a0cbc8f53d7349b3ba46a3bf1a948d2c))    The business postal code for the contact.

  **LastName** (section [2.2.2.45](#Section_494c41cf3db940c18d6a268139df9685))                     The contact\'s last name.

  **Spouse** (section [2.2.2.60](#Section_2c4197a9d47f4dbf858796860a0e48f6))                       The name of the contact\'s spouse/partner.

  **BusinessAddressState** (section [2.2.2.13](#Section_b2e507f1e40546ff980820544eee827d))         The business state for the contact.

  **BusinessAddressStreet** (section [2.2.2.14](#Section_125c48d3dd3c47f8ad41e1e04664dbba))        The business street address for the contact.

  **JobTitle** (section [2.2.2.44](#Section_ec1a7ad7449f42ddbd6d5c128fc9fb45))                     The contact\'s job title.

  **YomiFirstName** (section [2.2.2.66](#Section_bee4f9b870794dcf817eca348592ef50))                The Japanese phonetic rendering of the first name of the contact.

  **YomiLastName** (section [2.2.2.67](#Section_9957972d9c5e4c53ac31c0c26b3e353b))                 The Japanese phonetic rendering of the last name of the contact.

  **YomiCompanyName** (section [2.2.2.65](#Section_87b2476a61c84ff1b409dbfe895593e2))              The Japanese phonetic rendering of the company name for the contact.

  **OfficeLocation (**section [2.2.2.51](#Section_b58a3aa381314c7cbb73595271b8c2eb))               The office location for the contact.

  **RadioPhoneNumber** (section [2.2.2.59](#Section_e5b95d011faa462093a1ca091c0993ed))             The radio telephone number for the contact.

  **contacts2:CustomerId** (section [2.2.2.25](#Section_049831a2302647d2a834075373bfbed5))         The customer identifier (ID) for the contact.

  **contacts2:GovernmentId** (section [2.2.2.32](#Section_39c85091c08b40ebb25b8fec7bc24b2b))       The government-assigned identifier (ID) for the contact.

  **contacts2:IMAddress** (section [2.2.2.41](#Section_abeebb23b98f41ffafaf93febd0be94d))          The instant messaging address for the contact.

  **contacts2:IMAddress2** (section [2.2.2.42](#Section_3d15395768df40be883dabf87c02add0))         The alternative instant messaging address for the contact.

  **contacts2:IMAddress3** (section [2.2.2.43](#Section_cef23e3ddfbb453ba297bd33cdd101e7))         The tertiary instant messaging address for the contact.

  **contacts2:ManagerName** (section [2.2.2.46](#Section_5382ca24993a40419146c193cc7df6f6))        The [**distinguished name (DN)**](#gt_1175dd11-9368-41d5-98ed-d585f268ad4b) of the manager for the contact.

  **contacts2:CompanyMainPhone** (section [2.2.2.23](#Section_3d04a2e1cf344a5691cde9544c25ef6a))   The main telephone number for the contact\'s company.

  **contacts2:AccountName** (section [2.2.2.1](#Section_df6c57f0a51c4fa1b0a31826bdc740d8))         The account name and/or number for the contact.

  **contacts2:NickName** (section [2.2.2.50](#Section_19f9dd910b7d4e1cb7a81d56bf147332))           The nickname for the contact.

  **contacts2:MMS** (section [2.2.2.48](#Section_813b67d4d48549aab602a8623ad1843f))                The Multimedia Messaging Service (MMS) address for the contact.

  **Picture** (section [2.2.2.58](#Section_ac4faa8fb95941909cdcd787e1717dfb))                      The file, which is encoded with [**base64 encoding**](#gt_179b9392-9019-45a3-880b-26f6890522b7), containing the picture of the contact.

  **Categories** (section [2.2.2.19](#Section_8d739c5961814f0497f96b85f9687056))                   A collection of user labels assigned to the contact.

  **Category** (section [2.2.2.20](#Section_b22602cb04f648e698cefb2b3f428ded))                     A category that is assigned to the contact.

  **Children** (section [2.2.2.21](#Section_8e99b7e3deaf471ebbe012af745e26a5))                     A collection of the contact\'s children.

  **Child** (section [2.2.2.22](#Section_f6833f0f706c44ed86608e23871fd978))                        One of the contact's children.

  **airsyncbase:Body** (section [2.2.2.7.1](#Section_0d2f843a91c745e2bcfbf0ce67f9ef8b))            Specifies details about the notes for a contact.

  **contacts:Body** (section [2.2.2.7.2](#Section_626e75dfbd2644efbd5ae881f747de26))               Contains the notes for a contact that is retrieved from the server.

  **BodySize** (section [2.2.2.8](#Section_c5152413dab64cfca628f28ebf6f8169))                      Specifies the full size, in characters, of the contact notes.

  **BodyTruncated** (section [2.2.2.9](#Section_6ac6fc923f6d4ec5b3e4f6f1bdcd868d))                 Indicates whether the contact notes were truncated when sent from the server.
  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

#### AccountName

The **contacts2:AccountName** element specifies the account name and/or number for the contact. It is defined as an element in the **Contacts2** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Alias

The **Alias** element specifies the user\'s alias. It is defined as an element in the **Contacts** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

The **Alias** element MAY only be returned in a recipient information cache response. For more details about the interaction with the recipient information cache, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.186.3.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Anniversary

The **Anniversary** element specifies the wedding anniversary date for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **dateTime** data type in [**Coordinated Universal Time (UTC)**](#gt_f2369991-a884-4843-a8fa-1505b6d5ece7) format, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.3. The time portion of the **dateTime** value might be 11:59 and SHOULD be ignored, so that synchronizing between different time zones does not change the date.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### AssistantName

The **AssistantName** element specifies the name of the contact's assistant. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### AssistantPhoneNumber

The **AssistantPhoneNumber** element specifies the phone number of the contact\'s assistant. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.5.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Birthday

The **Birthday** element specifies the birth date of the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **dateTime** data type in [**Coordinated Universal Time (UTC)**](#gt_f2369991-a884-4843-a8fa-1505b6d5ece7) format, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.3. The time portion of the **dateTime** value might be 11:59 and SHOULD be ignored, so that synchronizing between different time zones does not change the date.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Body

The **Body** element is defined in the **Contacts** namespace, as specified in section [2.2.2.7.2](#Section_626e75dfbd2644efbd5ae881f747de26), for use by protocol version 2.5. It is defined in the **AirSyncBase** namespace, as specified in section [2.2.2.7.1](#Section_0d2f843a91c745e2bcfbf0ce67f9ef8b), for use by protocol versions 12.0, 12.1, 14.0, 14.1, and 16.0.

##### Body (AirSyncBase Namespace)

The **airsyncbase:Body** element is a **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that specifies the notes for the contact. It is defined as an element in the **AirSyncBase** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b). For more details about the **airsyncbase:Body** element, see [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.9.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

The **contacts:Body** element (section [2.2.2.7.2](#Section_626e75dfbd2644efbd5ae881f747de26)) is used instead of the **airsyncbase:Body** element with protocol version 2.5.

##### Body (Contacts Namespace)

The **Body** element is an optional element that contains the notes for a contact that is retrieved from the server. This element is defined in the **Contacts** namespace as a child of the **airsync:ApplicationData** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.11) in **Sync** command requests and responses (\[MS-ASCMD\] section 2.2.1.21).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

A client can use the **airsync:Truncation** element, as specified in \[MS-ASCMD\] section 2.2.3.185, to request truncation of the contact notes. This conserves space and reduces data traffic when synchronizing contacts. The server sets the **BodyTruncated** element (section [2.2.2.9](#Section_6ac6fc923f6d4ec5b3e4f6f1bdcd868d)) in the **Sync** response to indicate whether the contact notes have actually been truncated. The untruncated size of the contact notes is specified by the **BodySize** element (section [2.2.2.8](#Section_c5152413dab64cfca628f28ebf6f8169)).

When the client requests truncation, only the first part (or none) of each contact\'s notes is included in a synchronization. The complete notes for a contact cannot be retrieved after a contact has been synchronized with truncated notes.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                

  16.1                                
  -----------------------------------------------------------------------

The **airsyncbase:Body** element (section [2.2.2.7.1](#Section_0d2f843a91c745e2bcfbf0ce67f9ef8b)) is used instead of the **contacts:Body** element with all protocol versions except 2.5.

#### BodySize

The **BodySize** element is an optional element that specifies the full size, in characters, of the contact notes. This element is defined in the **Contacts** namespace as a child of the **airsync:ApplicationData** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.11) in **Sync** command responses (\[MS-ASCMD\] section 2.2.1.21).

The value of this element is an **integer** data type, as specified in as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

This element is present only when the **BodyTruncated** element (section [2.2.2.9](#Section_6ac6fc923f6d4ec5b3e4f6f1bdcd868d)) is set to 1. When the contact notes are truncated, the **BodySize** element is included to specify the original size of the contact notes prior to truncation.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                

  16.1                                
  -----------------------------------------------------------------------

#### BodyTruncated

The **BodyTruncated** element is an optional element that indicates whether the contact notes were truncated when sent from the server. This element is defined in the **Contacts** namespace as a child of the **airsync:ApplicationData** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.11) in **Sync** command responses (\[MS-ASCMD\] section 2.2.1.21).

The value of this element is an unsignedByte data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

A value of 1 indicates that the contact notes have been truncated by the server; a value of 0 (zero) indicates that the contact notes have not been truncated.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                

  16.1                                
  -----------------------------------------------------------------------

#### BusinessAddressCity

The **BusinessAddressCity** element specifies the business city of the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### BusinessAddressCountry

The **BusinessAddressCountry** element specifies the business country/region of the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### BusinessAddressPostalCode

The **BusinessAddressPostalCode** element specifies the business postal code for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### BusinessAddressState

The **BusinessAddressState** element specifies the business state for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### BusinessAddressStreet

The **BusinessAddressStreet** element specifies the business street address for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### BusinessFaxNumber

The **BusinessFaxNumber** element specifies the business fax number for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.5.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### BusinessPhoneNumber

The **BusinessPhoneNumber** element specifies the primary business phone number for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.5.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Business2PhoneNumber

The **Business2PhoneNumber** element specifies the secondary business telephone number for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.5.

This element can be ghosted. For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### CarPhoneNumber

The **CarPhoneNumber** element specifies the car telephone number for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.5.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Categories

The **Categories** element is a **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that specifies a collection of user labels assigned to the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The **Categories** element has the following child element:

-   **Category** (section [2.2.2.20](#Section_b22602cb04f648e698cefb2b3f428ded)): At least one instance of this element is required.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Category

The **Category** element is a required child element of the **Categories** element (section [2.2.2.19](#Section_8d739c5961814f0497f96b85f9687056)) that specifies a category that is assigned to the contact. It is defined as an element in the **Contacts** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

A command request or response has a minimum of one **Category** element per **Categories** element. It can have up to 300 elements per **Categories** element.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Children

The **Children** element is a **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that specifies a collection of the contact\'s children. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The **Children** element has the following child element:

-   **Child** (section [2.2.2.22](#Section_f6833f0f706c44ed86608e23871fd978)): This element is optional.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Child

The **Child** element is an optional child element of the **Children** element that specifies a child of the contact. It is defined as an element in the **Contacts** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

A command request or response has zero or more **Child** elements per **Children** element. It can have up to 300 elements per **Children** element.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### CompanyMainPhone

The **contacts2:CompanyMainPhone** element specifies the main telephone number for the contact\'s company. It is defined as an element in the **Contacts2** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.5.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### CompanyName

The **CompanyName** element specifies the company name for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### CustomerId

The **contacts2:CustomerId** element specifies the customer identifier (ID) for the contact. It is defined as an element in the **Contacts2** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Department

The **Department** element specifies the department name for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Email1Address

The **Email1Address** element specifies the first e-mail address for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.3.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

The **Email1Address** element is one of the **Contact** class elements that is returned in a recipient information cache response. For more details about interacting with the recipient information cache, see \[MS-ASCMD\] section 2.2.3.186.3.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Email2Address

The **Email2Address** element specifies the second e-mail address for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.3.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Email3Address

The **Email3Address** element specifies the third e-mail address for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.3.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### FileAs

The **FileAs** element specifies how a contact is filed in the [**Contacts folder**](#gt_b35ba7ae-4348-4a65-9d02-dabca97ccdec) or the [**recipient information cache**](#gt_4bcda02a-9b58-4abe-8c7d-d8336a130346) folder. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

Values for the **FileAs** element can differ depending on their location; the value for the **FileAs** element in the recipient information cache is not required to match the value of the **FileAs** element in the Contacts folder.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

The **FileAs** element is one of the **Contact** class elements that is returned in a recipient information cache response. For more details about the interaction with the recipient information cache, see \[MS-ASCMD\] section 2.2.3.186.3.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### FirstName

The **FirstName** element specifies the first name of the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### GovernmentId

The **contacts2:GovernmentId** element specifies the government-assigned identifier (ID) for the contact. It is defined as an element in the **Contacts2** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### HomeAddressCity

The **HomeAddressCity** element specifies the home city for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### HomeAddressCountry

The **HomeAddressCountry** element specifies the home country/region for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### HomeAddressPostalCode

The **HomeAddressPostalCode** element specifies the home postal code for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### HomeAddressState

The **HomeAddressState** element specifies the home state for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### HomeAddressStreet

The **HomeAddressStreet** element specifies the home street address for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### HomeFaxNumber

The **HomeFaxNumber** element specifies the home fax number for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.5.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### HomePhoneNumber

The **HomePhoneNumber** element specifies the home phone number for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.5.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Home2PhoneNumber

The **Home2PhoneNumber** element specifies the alternative home phone number for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.5.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### IMAddress

The **contacts2:IMAddress** element specifies the instant messaging address for the contact. It is defined as an element in the **Contacts2** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### IMAddress2

The **contacts2:IMAddress2** element specifies the alternative instant messaging address for the contact. It is defined as an element in the **Contacts2** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### IMAddress3

The **contacts2:IMAddress3** element specifies the tertiary instant messaging address for the contact. It is defined as an element in the **Contacts2** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### JobTitle

The **JobTitle** element specifies the contact's job title. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### LastName

The **LastName** element specifies the contact's last name. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### ManagerName

The **contacts2:ManagerName** element specifies the [**distinguished name (DN)**](#gt_1175dd11-9368-41d5-98ed-d585f268ad4b) of the contact\'s manager. It is defined as an element in the **Contacts2** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### MiddleName

The **MiddleName** element specifies the middle name of the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### MMS

The **contacts2:MMS** element specifies the Multimedia Messaging Service (MMS) address for the contact. It is defined as an element in the **Contacts2** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### MobilePhoneNumber

The **MobilePhoneNumber** element specifies the mobile phone number for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.5.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### NickName

The **contacts2:NickName** element specifies the nickname for the contact. It is defined as an element in the **Contacts2** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### OfficeLocation

The **OfficeLocation** element specifies the office location for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### OtherAddressCity

The **OtherAddressCity** element specifies the city for the contact\'s alternate address. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### OtherAddressCountry

The **OtherAddressCountry** element specifies the country/region of the contact's alternate address. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### OtherAddressPostalCode

The **OtherAddressPostalCode** element specifies the postal code of the contact's alternate address. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### OtherAddressState

The **OtherAddressState** element specifies the state of the contact's alternate address. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### OtherAddressStreet

The **OtherAddressStreet** element specifies the street address of the contact's alternate address. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### PagerNumber

The **PagerNumber** element specifies the pager number for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.5.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Picture

The **Picture** element specifies the file that contains the picture of the contact. The value of the **Picture** element SHOULD be a stream that is encoded with [**base64 encoding**](#gt_179b9392-9019-45a3-880b-26f6890522b7). It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

The value of the **Picture** element MUST be limited to 48 KB of binary content that is encoded with base64 encoding, or an image size of around 36 KB. Since base64 encoding is nondeterministic, the actual maximum size of the image can vary. If the value of the **Picture** element exceeds 48 KB of content with base64 encoding, the server MUST return a status error of 6.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### RadioPhoneNumber

The **RadioPhoneNumber** element specifies the radio phone number for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.5.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Spouse

The **Spouse** element specifies the name of the contact's spouse/partner. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Suffix

The **Suffix** element specifies the suffix for the contact's name. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Title

The **Title** element specifies the contact's business title. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### WebPage

The **WebPage** element specifies the Web site or personal Web page for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### WeightedRank

The **WeightedRank** element specifies the rank of this contact entry in the recipient information cache. It is defined as an element in the **Contacts** namespace.

The value of this element is an **integer** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

Clients can use the **WeightedRank** element to determine which entries in a recipient information cache list are displayed first in an auto-completion field. Higher values of the **WeightedRank** element identify the most relevant entries.

The **WeightedRank** element is only returned in a recipient information cache response. For more details about the interaction with the recipient information cache, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.186.3.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### YomiCompanyName

The **YomiCompanyName** element specifies the Japanese phonetic rendering of the company name for the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### YomiFirstName

The **YomiFirstName** element specifies the Japanese phonetic rendering of the first name of the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### YomiLastName

The **YomiLastName** element specifies the Japanese phonetic rendering of the last name of the contact. It is defined as an element in the **Contacts** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 Yes

  12.0                                Yes

  12.1                                Yes

  14.0                                Yes

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

# Protocol Details

## Client Details

### Abstract Data Model

This section describes a conceptual model of possible data organization that an implementation maintains to participate in this protocol. The described organization is provided to facilitate the explanation of how the protocol behaves. This document does not mandate that implementations adhere to this model as long as their external behavior is consistent with that described in this document.

**Contact** **class**: A structured XML text block that specifies a contact and adheres to the [**XML schema**](#gt_bd0ce6f9-c350-4900-827e-951265294067) specified in section [2.2](#Section_b9e289337b0f4787a244feed0b18f7bd). It is returned by the server to the client as part of a full XML response to the client command requests specified in section [3.1.5](#Section_9ab08e89e1d2454db2c66a75755d7074).

**Command request:** A [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc) formatted message that adheres to the command schemas specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

### Timers

None.

### Initialization

None.

### Higher-Layer Triggered Events

#### Synchronizing Contact Data Between Client and Server

A client initiates synchronization of **Contact** class data with the server by sending a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) to the server.

#### Searching a Server for Contact Data

A client searches for **Contact** class data by sending a **Search** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.16) to the server.

#### Requesting Details for Specific Contacts

A client requests **Contact** class data for one or more contacts by sending an **ItemOperations** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10) to the server that contains one or more **itemoperations:Fetch** elements (\[MS-ASCMD\] section 2.2.3.67.1).

#### Refreshing the Recipient Information Cache

A client retrieves a minimal set of **Contact** class data from the server by issuing a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) against [**Folder object**](#gt_0682daa7-c1b8-419b-8a32-6048833d0b72) type 19, which is the recipient information cache. The recipient information cache is not supported by protocol versions 2.5, 12.0, and 12.1.

For more details about the use of this Folder object type in a **Sync** command request, see \[MS-ASCMD\] section 2.2.3.30.6.

### Message Processing Events and Sequencing Rules

The following sections define how various elements of the **Contact** class are used in the context of specific ActiveSync commands. Command details are specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

#### ItemOperations Command Request

A client uses an **ItemOperations** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10) that contains one or more **itemoperations:Fetch** elements (\[MS-ASCMD\] section 2.2.3.67.1) to retrieve data from the server for one or more contact items.

Any of the elements that belong to the **Contact** class, as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b), can be included in an **ItemOperations** command request.

The client can restrict the elements returned by the **ItemOperations** command response (\[MS-ASCMD\] section 2.2.1.10) by including top-level schema elements for the **Contact** class as child elements of the **itemoperations:Schema** element (\[MS-ASCMD\] section 2.2.3.158) in the **ItemOperations** command request. For the **Contact** class, every element is considered a top-level schema element.

The **ItemOperations** command is specified in \[MS-ASCMD\] section 2.2.1.10.

The client can request that the server truncate the contact notes field in an **ItemOperations** command request. For more information, see section [3.1.5.4](#Section_1dc5d626a13c450aa96b06a0f088c44e).

#### Search Command Request

A client uses the **Search** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.16) to retrieve **Contact** class items that match the criteria specified by the client.

None of the elements that belong to the **Contact** class, as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b), can be included in a **Search** command request.

The **Search** command is specified in \[MS-ASCMD\] section 2.2.1.16.

#### Sync Command Request

A client uses the **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) to synchronize its **Contact** class items for a specified user with the contacts currently stored by the server.

Any of the elements that belong to the **Contact** class, as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b), can be included in a **Sync** command request as child elements of the **airsync:ApplicationData** element (\[MS-ASCMD\] section 2.2.3.11) within either an **airsync:Add** element (\[MS-ASCMD\] section 2.2.3.7.2) or an **airsync:Change** element (\[MS-ASCMD\] section 2.2.3.24).

**Contact** class elements can be transmitted as child elements of the **Supported** element (\[MS-ASCMD\] section 2.2.3.179) in order to support [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5) elements.[\<1\>](\l)

The **Sync** command is specified in \[MS-ASCMD\] section 2.2.1.21.

The client can request that the server truncate the contact notes field in a **Sync** command request. For more information, see section [3.1.5.4](#Section_1dc5d626a13c450aa96b06a0f088c44e).

##### Omitting Ghosted Properties from a Sync Change Request

At the beginning of a session (that is, when the value of the **SyncKey** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.181.4) in a **Sync** command request (\[MS-ASCMD\] section 2.2.1.21) is 0 (zero)), the client uses the **airsync:Supported** element (\[MS-ASCMD\] section 2.2.3.179) in the **Sync** command request to specify which properties are not [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). In subsequent **Sync** command requests, the client includes only the set of **airsync:Supported** elements from the **Sync** command request\'s **airsync:Change** element.

For more information on ghosted properties, see \[MS-ASCMD\] section 2.2.3.179.

#### Truncating the Contact Notes Field

A client can request that the server truncate the contents of the **airsyncbase:Body** element (section [2.2.2.7.1](#Section_0d2f843a91c745e2bcfbf0ce67f9ef8b)) in the **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) or **ItemOperations** command response (\[MS-ASCMD\] section 2.2.1.10) by including the **airsyncbase:TruncationSize** element ([\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.40.2) in a **Sync** command request (\[MS-ASCMD\] section 2.2.1.21) or **ItemOperations** command request (\[MS-ASCMD\] section 2.2.1.10). The behavior of **airsyncbase:TruncationSize** is specified in \[MS-ASAIRS\] section 2.2.2.40.2.

Once a client requests truncation, the server truncates the contents of the **airsyncbase:Body** element in all subsequent **Sync** command responses. A client can request that the server no longer truncate the contents of the **airsyncbase:Body** element by sending an **airsyncbase:BodyPreference** element (\[MS-ASAIRS\] section 2.2.2.12) in the request that contains a **Type** element (\[MS-ASAIRS\] section 2.2.2.41.4) to specify the desired format, but does not include the **airsyncbase:TruncationSize** element.

If an **airsyncbase:Body** element is not included in the request that is sent from the client to the server, the server MUST NOT delete the stored Notes for the contact.

Client devices that do not support the notes field for contacts can omit the **airsyncbase:Body** element when synchronizing contact information with a server.

### Timer Events

None.

### Other Local Events

None.

## Server Details

### Abstract Data Model

This section describes a conceptual model of possible data organization that an implementation maintains to participate in this protocol. The described organization is provided to facilitate the explanation of how the protocol behaves. This document does not mandate that implementations adhere to this model as long as their external behavior is consistent with that described in this document.

**Contact** **class:** A structured [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) text block that specifies a contact and adheres to the [**XML schema**](#gt_bd0ce6f9-c350-4900-827e-951265294067) specified in section [2.2](#Section_b9e289337b0f4787a244feed0b18f7bd). It is returned by the server as part of a full XML response to the client requests specified in section [3.1.5](#Section_9ab08e89e1d2454db2c66a75755d7074).

**Command response:** A [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc) formatted message that adheres to the command schemas specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

### Timers

None.

### Initialization

None.

### Higher-Layer Triggered Events

#### Synchronizing Contact Data Between Client and Server

Synchronization of **Contact** class data between client and server is initiated by the client, as specified in section [3.1.4.1](#Section_0ccd667c8c194aea9d9188de83c7a2ec). The server responds with a **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21).

#### Searching for Contact Data

Searching for **Contact** class data is initiated by the client, as specified in section [3.1.4.2](#Section_e8bfee5010794ae4ac1bf2e315824358). The server responds with a **Search** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.16).

#### Retrieving Details for Specific Contacts

Retrieval of **Contact** class data for one or more contact items is initiated by the client, as specified in section [3.1.4.3](#Section_687059c80194413ba5fce1b4184ec2e4). The server responds with an **ItemOperations** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10).

#### Refreshing the Recipient Information Cache

Retrieval of a minimal set of **Contact** class data that represents the recipient information cache is initiated by the client, as specified in section [3.1.4.4](#Section_6fdfe80e155c4811b58034ee389b4732). The recipient information cache is not supported by protocol versions 2.5, 12.0, and 12.1. The server responds with a **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) that includes only the following elements from the **Contact** class:

-   **Email1Address** (section [2.2.2.27](#Section_91bd445da02a4b99ab530208087e7ac8))

-   **FileAs** (section [2.2.2.30](#Section_f0384e7f7a974177b84e60dcdec15b78))

-   **Alias** (section [2.2.2.2](#Section_53e4357a0faf40698361912cfe9ddb39))

-   **WeightedRank** (section [2.2.2.64](#Section_bc96fc4515f84c42aff3400ddd953584))

This use of the **Sync** command is further specified in \[MS-ASCMD\] section 2.2.3.30.6.

### Message Processing Events and Sequencing Rules

The following sections define how various elements of the **Contact** class are used in the context of specific ActiveSync commands. Command details are specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

#### ItemOperations Command Response

When a client uses an **ItemOperations** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10) to retrieve data from the server for one or more contact items, as specified in section [3.1.5.1](#Section_3aa2b0d9ebec4da2b827050aedb7ed26), the server responds with an **ItemOperations** command response (\[MS-ASCMD\] section 2.2.1.10).

Any of the elements that belong to the **Contact** class, as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b), can be included in an **ItemOperations** command response. If an **itemoperations:Schema** element (\[MS-ASCMD\] section 2.2.3.158) is included in the **ItemOperations** command request, the elements returned in the **ItemOperations** command response **MUST** be restricted to the elements that were included as child elements of the **itemoperations:Schema** element in the command request.

**Contact** class elements are returned as child elements of the **itemoperations:Properties** element (\[MS-ASCMD\] section 2.2.3.139) in the **ItemOperations** command response.

The **ItemOperations** command is specified in \[MS-ASCMD\] section 2.2.1.10.

#### Search Command Response

When a client uses the **Search** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.16) to retrieve **Contact** class items that match the criteria specified by the client, as specified in section [3.1.5.2](#Section_8f7ee7b12c3c479aa7c5e3a89f7660c1), the server responds with a **Search** command response (\[MS-ASCMD\] section 2.2.1.16).

Any of the elements that belong to the **Contact** class, as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b), can be included in a **Search** command response.

**Contact** class elements are returned as child elements of the **search:Properties** element (\[MS-ASCMD\] section 2.2.3.139) in the **Search** command response.

The **Search** command is specified in \[MS-ASCMD\] section 2.2.1.16.

#### Sync Command Response

When a client uses the **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) to synchronize its **Contact** class items for a specified user with the contacts currently stored by the server, as specified in section [3.1.5.3](#Section_1168e35fbec340769ce0aea2c355df30), the server responds with a **Sync** command response (\[MS-ASCMD\] section 2.2.1.21).

Any of the elements that belong to the **Contact** class, as specified in section [2.2.2](#Section_a006c6886d5f4aeda9acd70019d3c67b), can be included in a **Sync** command response as child elements of the **airsync:ApplicationData** element (\[MS-ASCMD\] section 2.2.3.11) within either an **airsync:Add** element (\[MS-ASCMD\] section 2.2.3.7.2) or an **airsync:Change** element (\[MS-ASCMD\] section 2.2.3.24).

The **Sync** command is specified in \[MS-ASCMD\] section 2.2.1.21.

##### Omitting Ghosted Properties from a Sync Change Request

At the beginning of a session (that is, when the value of the **SyncKey** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.181.4) in a **Sync** command request (\[MS-ASCMD\] section 2.2.1.21) is 0 (zero))), the client uses the **airsync:Supported** element (\[MS-ASCMD\] section 2.2.3.179) in the **Sync** command request to specify which properties are not [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). In subsequent **Sync** command requests, the client includes only these elements from the **Sync** request\'s **airsync:Change** element. Ghosted elements are not sent to the server. Instead of deleting these excluded properties, the server preserves their previous value.

For more details about ghosted properties, see \[MS-ASCMD\] section 2.2.3.179.

### Timer Events

None.

### Other Local Events

None.

# Protocol Examples

The following example demonstrates a client request to synchronize contact data with the server, and the server response. In this example, the server returns a single new contact, represented by elements of the **Contact** class that are child elements of the **airsync:ApplicationData** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.11) under an **airsync:Add** element (\[MS-ASCMD\] section 2.2.3.7.2) in the server response.

**Note** For the sake of brevity, the value of the **Picture** element in the server response, which is a representation of the image encoded with [**base64 encoding**](#gt_179b9392-9019-45a3-880b-26f6890522b7), has been truncated.

Request:

1.  \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

    \<Sync xmlns=\"AirSync\"\>

    \<Collections\>

    \<Collection\>

    \<SyncKey\>2006814013\</SyncKey\>

    \<CollectionId\>2\</CollectionId\>

    \<DeletesAsMoves/\>

    \<GetChanges/\>

    \</Collection\>

    \</Collections\>

    \</Sync\>

Response:

12. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

    \<Sync xmlns=\"AirSync\" xmlns:A=\"AirSyncBase\" xmlns:B=\"POOMCONTACTS\"\>

    \<Collections\>

    \<Collection\>

    \<SyncKey\>243360144\</SyncKey\>

    \<CollectionId\>2\</CollectionId\>

    \<Status\>1\</Status\>

    \<Commands\>

    \<Add\>

    \<ServerId\>2:1\</ServerId\>

    \<ApplicationData\>

    \<A:Body\>

    \<A:Type\>3\</A:Type\>

    \<A:EstimatedDataSize\>5500\</A:EstimatedDataSize\>

    \<A:Truncated\>1\</A:Truncated\>

    \</A:Body\>

    \<B:WebPage\>http://www.contoso.com/\</B:WebPage\>

    \<B:BusinessAddressCountry\>United States of America\</B:BusinessAddressCountry\>

    \<B:Email1Address\>\"Anat Kerry (anat@contoso.com)\" &lt;anat@contoso.com&gt;\</B:Email1Address\>

    \<B:BusinessFaxNumber\>(206) 555-0100\</B:BusinessFaxNumber\>

    \<B:FileAs\>Kerry, Anat\</B:FileAs\>

    \<B:FirstName\>Anat\</B:FirstName\>

    \<B:HomePhoneNumber\>(206) 555-0101\</B:HomePhoneNumber\>

    \<B:BusinessAddressCity\>Redmond\</B:BusinessAddressCity\>

    \<B:MiddleName\>M.\</B:MiddleName\>

    \<B:MobilePhoneNumber\>(206) 555-0102\</B:MobilePhoneNumber\>

    \<B:CompanyName\>Contoso, Ltd.\</B:CompanyName\>

    \<B:BusinessAddressPostalCode\>10021\</B:BusinessAddressPostalCode\>

    \<B:LastName\>Kerry\</B:LastName\>

    \<B:BusinessAddressState\>WA\</B:BusinessAddressState\>

    \<B:BusinessAddressStreet\>234 Main St.\</B:BusinessAddressStreet\>

    \<B:BusinessPhoneNumber\>(206) 555-0103\</B:BusinessPhoneNumber\>

    \<B:JobTitle\>Development Manager\</B:JobTitle\>

    \<B:Picture\>/9j/4AAQSkZJRgABAQEAYABgAAD/\...\</B:Picture\>

    \<A:NativeBodyType\>3\</A:NativeBodyType\>

    \</ApplicationData\>

    \</Add\>

    \</Commands\>

    \</Collection\>

    \</Collections\>

    \</Sync\>

# Security

## Security Considerations for Implementers

None.

## Index of Security Parameters

None.

# Appendix A: Full XML Schema

For ease of implementation, the following sections provide the full [**XML schemas**](#gt_bd0ce6f9-c350-4900-827e-951265294067) for this protocol. These schemas are valid for protocol versions 2.5, 12.0, 12.1, 14.0, 14.1, 16.0 and 16.1.

  -----------------------------------------------------------------------------------------------------------
  Schema name                      Prefix                  Section
  -------------------------------- ----------------------- --------------------------------------------------
  **Contacts** namespace schema    contacts                [6.1](#Section_9ff6d8b916c1467ab522ce9637197966)

  **Contacts2** namespace schema   contacts2               [6.2](#Section_cdeba6736e764c4a929618bd40385a30)
  -----------------------------------------------------------------------------------------------------------

## Contacts Namespace Schema

This section contains the contents of the Contacts.xsd file. The additional file that this schema file requires to operate correctly is listed in the following table.

  -----------------------------------------------------------------------------------------------------------------------------
  File name                           Defining specification
  ----------------------------------- -----------------------------------------------------------------------------------------
  AirSyncBase.xsd                     [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 6

  -----------------------------------------------------------------------------------------------------------------------------

53. \<?xml version=\"1.0\" encoding=\"UTF-8\"?\>

    \<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:airsyncbase=

    \"AirSyncBase\" xmlns=\"Contacts\" targetNamespace=\"Contacts\"

    elementFormDefault=\"qualified\" attributeFormDefault=\"unqualified\"\>

    \<xs:import namespace=\"AirSyncBase\" schemaLocation=\"AirSyncBase.xsd\"/\>

    \<xs:element name=\"Anniversary\" type=\"xs:dateTime\"/\>

    \<xs:element name=\"AssistantName\" type=\"xs:string\"/\>

    \<xs:element name=\"AssistantPhoneNumber\" type=\"xs:string\"/\>

    \<xs:element name=\"Birthday\" type=\"xs:dateTime\"/\>

    \<xs:element name=\"Body\" type=\"xs:string\"/\>

    \<xs:element name=\"BodySize\" type=\"xs:integer\"/\>

    \<xs:element name=\"BodyTruncated\" type=\"xs:unsignedByte\"/\>

    \<xs:element name=\"Business2PhoneNumber\" type=\"xs:string\"/\>

    \<xs:element name=\"BusinessAddressCity\" type=\"xs:string\"/\>

    \<xs:element name=\"BusinessPhoneNumber\" type=\"xs:string\"/\>

    \<xs:element name=\"WebPage\" type=\"xs:string\"/\>

    \<xs:element name=\"BusinessAddressCountry\" type=\"xs:string\"/\>

    \<xs:element name=\"Department\" type=\"xs:string\"/\>

    \<xs:element name=\"Email1Address\" type=\"xs:string\"/\>

    \<xs:element name=\"Email2Address\" type=\"xs:string\"/\>

    \<xs:element name=\"Email3Address\" type=\"xs:string\"/\>

    \<xs:element name=\"BusinessFaxNumber\" type=\"xs:string\"/\>

    \<xs:element name=\"FileAs\" type=\"xs:string\"/\>

    \<xs:element name=\"Alias\" type=\"xs:string\"/\>

    \<xs:element name=\"WeightedRank\" type=\"xs:int\"/\>

    \<xs:element name=\"FirstName\" type=\"xs:string\"/\>

    \<xs:element name=\"MiddleName\" type=\"xs:string\"/\>

    \<xs:element name=\"HomeAddressCity\" type=\"xs:string\"/\>

    \<xs:element name=\"HomeAddressCountry\" type=\"xs:string\"/\>

    \<xs:element name=\"HomeFaxNumber\" type=\"xs:string\"/\>

    \<xs:element name=\"HomePhoneNumber\" type=\"xs:string\"/\>

    \<xs:element name=\"Home2PhoneNumber\" type=\"xs:string\"/\>

    \<xs:element name=\"HomeAddressPostalCode\" type=\"xs:string\"/\>

    \<xs:element name=\"HomeAddressState\" type=\"xs:string\"/\>

    \<xs:element name=\"HomeAddressStreet\" type=\"xs:string\"/\>

    \<xs:element name=\"MobilePhoneNumber\" type=\"xs:string\"/\>

    \<xs:element name=\"Suffix\" type=\"xs:string\"/\>

    \<xs:element name=\"CompanyName\" type=\"xs:string\"/\>

    \<xs:element name=\"OtherAddressCity\" type=\"xs:string\"/\>

    \<xs:element name=\"OtherAddressCountry\" type=\"xs:string\"/\>

    \<xs:element name=\"CarPhoneNumber\" type=\"xs:string\"/\>

    \<xs:element name=\"OtherAddressPostalCode\" type=\"xs:string\"/\>

    \<xs:element name=\"OtherAddressState\" type=\"xs:string\"/\>

    \<xs:element name=\"OtherAddressStreet\" type=\"xs:string\"/\>

    \<xs:element name=\"PagerNumber\" type=\"xs:string\"/\>

    \<xs:element name=\"Title\" type=\"xs:string\"/\>

    \<xs:element name=\"BusinessAddressPostalCode\" type=\"xs:string\"/\>

    \<xs:element name=\"LastName\" type=\"xs:string\"/\>

    \<xs:element name=\"Spouse\" type=\"xs:string\"/\>

    \<xs:element name=\"BusinessAddressState\" type=\"xs:string\"/\>

    \<xs:element name=\"BusinessAddressStreet\" type=\"xs:string\"/\>

    \<xs:element name=\"JobTitle\" type=\"xs:string\"/\>

    \<xs:element name=\"YomiFirstName\" type=\"xs:string\"/\>

    \<xs:element name=\"YomiLastName\" type=\"xs:string\"/\>

    \<xs:element name=\"YomiCompanyName\" type=\"xs:string\"/\>

    \<xs:element name=\"OfficeLocation\" type=\"xs:string\"/\>

    \<xs:element name=\"RadioPhoneNumber\" type=\"xs:string\"/\>

    \<xs:element name=\"Picture\" type=\"xs:string\"/\>

    \<xs:element name=\"Categories\"\>

    \<xs:complexType\>

    \<xs:sequence\>

    \<xs:element name=\"Category\" type=\"xs:string\" minOccurs=\"0\"

    maxOccurs=\"300\"/\>

    \</xs:sequence\>

    \</xs:complexType\>

    \</xs:element\>

    \<xs:element name=\"Children\"\>

    \<xs:complexType\>

    \<xs:sequence minOccurs=\"0\"\>

    \<xs:element name=\"Child\" type=\"xs:string\" minOccurs=\"0\"

    maxOccurs=\"300\"/\>

    \</xs:sequence\>

    \</xs:complexType\>

    \</xs:element\>

    \<xs:group name=\"AllProps\"\>

    \<xs:sequence\>

    \<xs:choice maxOccurs=\"unbounded\"\>

    \<xs:element ref=\"Anniversary\"/\>

    \<xs:element ref=\"AssistantName\"/\>

    \<xs:element ref=\"AssistantPhoneNumber\"/\>

    \<xs:element ref=\"Birthday\"/\>

    \<xs:element ref=\"Body\"/\>

    \<xs:element ref=\"BodySize\"/\>

    \<xs:element ref=\"BodyTruncated\"/\>

    \<xs:element ref=\"Business2PhoneNumber\"/\>

    \<xs:element ref=\"BusinessAddressCity\"/\>

    \<xs:element ref=\"BusinessPhoneNumber\"/\>

    \<xs:element ref=\"WebPage\"/\>

    \<xs:element ref=\"BusinessAddressCountry\"/\>

    \<xs:element ref=\"Department\"/\>

    \<xs:element ref=\"Email1Address\"/\>

    \<xs:element ref=\"Email2Address\"/\>

    \<xs:element ref=\"Email3Address\"/\>

    \<xs:element ref=\"BusinessFaxNumber\"/\>

    \<xs:element ref=\"FileAs\"/\>

    \<xs:element ref=\"Alias\"/\>

    \<xs:element ref=\"WeightedRank\"/\>

    \<xs:element ref=\"FirstName\"/\>

    \<xs:element ref=\"MiddleName\"/\>

    \<xs:element ref=\"HomeAddressCity\"/\>

    \<xs:element ref=\"HomeAddressCountry\"/\>

    \<xs:element ref=\"HomeFaxNumber\"/\>

    \<xs:element ref=\"HomePhoneNumber\"/\>

    \<xs:element ref=\"Home2PhoneNumber\"/\>

    \<xs:element ref=\"HomeAddressPostalCode\"/\>

    \<xs:element ref=\"HomeAddressState\"/\>

    \<xs:element ref=\"HomeAddressStreet\"/\>

    \<xs:element ref=\"MobilePhoneNumber\"/\>

    \<xs:element ref=\"Suffix\"/\>

    \<xs:element ref=\"CompanyName\"/\>

    \<xs:element ref=\"OtherAddressCity\"/\>

    \<xs:element ref=\"OtherAddressCountry\"/\>

    \<xs:element ref=\"CarPhoneNumber\"/\>

    \<xs:element ref=\"OtherAddressPostalCode\"/\>

    \<xs:element ref=\"OtherAddressState\"/\>

    \<xs:element ref=\"OtherAddressStreet\"/\>

    \<xs:element ref=\"PagerNumber\"/\>

    \<xs:element ref=\"Title\"/\>

    \<xs:element ref=\"BusinessAddressPostalCode\"/\>

    \<xs:element ref=\"LastName\"/\>

    \<xs:element ref=\"Spouse\"/\>

    \<xs:element ref=\"BusinessAddressState\"/\>

    \<xs:element ref=\"BusinessAddressStreet\"/\>

    \<xs:element ref=\"JobTitle\"/\>

    \<xs:element ref=\"YomiFirstName\"/\>

    \<xs:element ref=\"YomiLastName\"/\>

    \<xs:element ref=\"YomiCompanyName\"/\>

    \<xs:element ref=\"OfficeLocation\"/\>

    \<xs:element ref=\"RadioPhoneNumber\"/\>

    \<xs:element ref=\"Picture\"/\>

    \<xs:element ref=\"Categories\"/\>

    \<xs:element ref=\"Children\"/\>

    \</xs:choice\>

    \</xs:sequence\>

    \</xs:group\>

    \<xs:group name=\"GhostingProps\"\>

    \<xs:sequence\>

    \<xs:choice maxOccurs=\"unbounded\"\>

    \<xs:element name=\"Anniversary\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Birthday\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"WebPage\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Children\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"BusinessAddressCountry\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Department\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Email1Address\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Email2Address\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Email3Address\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"BusinessFaxNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"FileAs\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"FirstName\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"HomeAddressCity\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"HomeAddressCountry\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"HomeFaxNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"HomePhoneNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Home2PhoneNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"HomeAddressPostalCode\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"HomeAddressState\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"HomeAddressStreet\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"BusinessAddressCity\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"MiddleName\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"MobilePhoneNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Suffix\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"CompanyName\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"OtherAddressCity\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"OtherAddressCountry\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"CarPhoneNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"OtherAddressPostalCode\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"OtherAddressState\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"OtherAddressStreet\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"PagerNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Title\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"BusinessAddressPostalCode\"

    type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"AssistantName\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"AssistantPhoneNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"AssistnamePhoneNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"LastName\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Spouse\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"BusinessAddressState\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"BusinessAddressStreet\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"BusinessPhoneNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Business2PhoneNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"JobTitle\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"YomiFirstName\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"YomiLastName\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"YomiCompanyName\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"OfficeLocation\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"RadioPhoneNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Picture\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Categories\" type=\"airsyncbase:EmptyTag\"/\>

    \</xs:choice\>

    \</xs:sequence\>

    \</xs:group\>

    \<xs:group name=\"TopLevelSchemaProps\"\>

    \<xs:sequence\>

    \<xs:choice maxOccurs=\"unbounded\"\>

    \<xs:element name=\"Anniversary\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Birthday\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Webpage\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Children\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"BusinessAddressCountry\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Department\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Email1Address\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Email2Address\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Email3Address\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"BusinessFaxNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"FileAs\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"FirstName\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"HomeAddressCity\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"HomeAddressCountry\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"HomeFaxNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"HomeTelephoneNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Home2TelephoneNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"HomeAddressPostalCode\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"HomeAddressState\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"HomeAddressStreet\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"BusinessAddressCity\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"MiddleName\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"MobileTelephoneNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Suffix\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"CompanyName\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"OtherAddressCity\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"OtherAddressCountry\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"CarTelephoneNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"OtherAddressPostalCode\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"OtherAddressState\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"OtherAddressStreet\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"PagerNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Title\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"BusinessAddressPostalCode\"

    type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"AssistantName\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"AssistantTelephoneNumber\"

    type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"LastName\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Spouse\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"BusinessAddressState\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"BusinessAddressStreet\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"BusinessTelephoneNumber\"

    type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Business2TelephoneNumber\"

    type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"JobTitle\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"YomiFirstName\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"YomiLastName\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"YomiCompanyName\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"OfficeLocation\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"RadioTelephoneNumber\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Categories\" type=\"airsyncbase:EmptyTag\"/\>

    \<xs:element name=\"Picture\" type=\"airsyncbase:EmptyTag\"/\>

    \</xs:choice\>

    \</xs:sequence\>

    \</xs:group\>

    \</xs:schema\>

## Contacts2 Namespace Schema

This section contains the contents of the Contacts2.xsd file. The additional file that this schema file requires to operate correctly is listed in the following table.

  -----------------------------------------------------------------------------------------------------------------------------
  File name                           Defining specification
  ----------------------------------- -----------------------------------------------------------------------------------------
  AirSyncBase.xsd                     [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 6

  -----------------------------------------------------------------------------------------------------------------------------

307. \<?xml version=\"1.0\" encoding=\"UTF-8\"?\>

     \<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:airsyncbase=

     \"AirSyncBase\" xmlns=\"Contacts2\" targetNamespace=\"Contacts2\"

     elementFormDefault=\"qualified\" attributeFormDefault=\"unqualified\"\>

     \<xs:import namespace=\"AirSyncBase\" schemaLocation=\"AirSyncBase.xsd\"/\>

     \<xs:element name=\"CustomerId\" type=\"xs:string\"/\>

     \<xs:element name=\"GovernmentId\" type=\"xs:string\"/\>

     \<xs:element name=\"IMAddress\" type=\"xs:string\"/\>

     \<xs:element name=\"IMAddress2\" type=\"xs:string\"/\>

     \<xs:element name=\"IMAddress3\" type=\"xs:string\"/\>

     \<xs:element name=\"ManagerName\" type=\"xs:string\"/\>

     \<xs:element name=\"CompanyMainPhone\" type=\"xs:string\"/\>

     \<xs:element name=\"AccountName\" type=\"xs:string\"/\>

     \<xs:element name=\"NickName\" type=\"xs:string\"/\>

     \<xs:element name=\"MMS\" type=\"xs:string\"/\>

     \<xs:group name=\"AllProps\"\>

     \<xs:sequence\>

     \<xs:choice maxOccurs=\"unbounded\"\>

     \<xs:element ref=\"CustomerId\"/\>

     \<xs:element ref=\"GovernmentId\"/\>

     \<xs:element ref=\"IMAddress\"/\>

     \<xs:element ref=\"IMAddress2\"/\>

     \<xs:element ref=\"IMAddress3\"/\>

     \<xs:element ref=\"ManagerName\"/\>

     \<xs:element ref=\"CompanyMainPhone\"/\>

     \<xs:element ref=\"AccountName\"/\>

     \<xs:element ref=\"NickName\"/\>

     \<xs:element ref=\"MMS\"/\>

     \</xs:choice\>

     \</xs:sequence\>

     \</xs:group\>

     \<xs:group name=\"GhostingProps\"\>

     \<xs:sequence\>

     \<xs:choice maxOccurs=\"unbounded\"\>

     \<xs:element name=\"CustomerId\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"GovernmentId\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"IMAddress\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"IMAddress2\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"IMAddress3\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"ManagerName\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"CompanyMainPhone\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"AccountName\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"NickName\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"MMS\" type=\"airsyncbase:EmptyTag\"/\>

     \</xs:choice\>

     \</xs:sequence\>

     \</xs:group\>

     \<xs:group name=\"TopLevelSchemaProps\"\>

     \<xs:sequence\>

     \<xs:choice maxOccurs=\"unbounded\"\>

     \<xs:element name=\"CustomerId\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"GovernmentId\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"IMAddress\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"IMAddress2\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"IMAddress3\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"ManagerName\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"CompanyMainPhone\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"AccountName\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"NickName\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"MMS\" type=\"airsyncbase:EmptyTag\"/\>

     \</xs:choice\>

     \</xs:sequence\>

     \</xs:group\>

     \</xs:schema\>

# Appendix B: Product Behavior

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

[\<1\> Section 3.1.5.3](\l): The **Sync** command returns a **Status** value of 4 when the **Supported** element is included in a **Sync** request that is sent to an Exchange 2007 SP1 server.

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
  [7](#Section_054796c454b44ce6a569d6db232b0033) Appendix B: Product Behavior   Updated list of supported products.   Major

  ------------------------------------------------------------------------------------------------------------------------------------

# Index

A

Abstract data model

[client](#abstract-data-model) 56

[server](#abstract-data-model-1) 58

[Applicability](#applicability-statement) 9

C

[Capability negotiation](#versioning-and-capability-negotiation) 9

[Change tracking](#change-tracking) 71

Client

[abstract data model](#abstract-data-model) 56

[initialization](#initialization) 56

[message processing](#message-processing-events-and-sequencing-rules) 57

[other local events](#other-local-events) 58

[sequencing rules](#message-processing-events-and-sequencing-rules) 57

[timer events](#timer-events) 58

[timers](#timers) 56

Contacts Namespace Schema schema

[Full XML Schema:\\Contacts Namespace Schema schema](#contacts-namespace-schema) 64

Contacts2 Namespace Schema schema

[Full XML schema:\\Contacts2 Namespace Schema schema](#contacts2-namespace-schema) 68

D

Data model - abstract

[client](#abstract-data-model) 56

[server](#abstract-data-model-1) 58

E

Elements

[AccountName](#accountname) 13

[Alias](#alias) 14

[Anniversary](#anniversary) 14

[AssistantName](#assistantname) 15

[AssistantPhoneNumber](#assistantphonenumber) 16

[Birthday](#birthday) 16

[Body](#body-airsyncbase-namespace) 17

[Business2PhoneNumber](#business2phonenumber) 24

[BusinessAddressCity](#businessaddresscity) 20

[BusinessAddressCountry](#businessaddresscountry) 20

[BusinessAddressPostalCode](#businessaddresspostalcode) 21

[BusinessAddressState](#businessaddressstate) 21

[BusinessAddressStreet](#businessaddressstreet) 22

[BusinessFaxNumber](#businessfaxnumber) 23

[BusinessPhoneNumber](#businessphonenumber) 23

[CarPhoneNumber](#carphonenumber) 24

[Categories](#categories) 25

[Category](#category) 26

[Child](#child) 27

[Children](#children) 26

[CompanyMainPhone](#companymainphone) 27

[CompanyName](#companyname) 28

[CustomerId](#customerid) 29

[Department](#department) 29

[Email1Address](#email1address) 30

[Email2Address](#email2address) 31

[Email3Address](#email3address) 31

[FileAs](#fileas) 32

[FirstName](#firstname) 33

[GovernmentId](#governmentid) 33

[Home2PhoneNumber](#home2phonenumber) 38

[HomeAddressCity](#homeaddresscity) 34

[HomeAddressCountry](#homeaddresscountry) 34

[HomeAddressPostalCode](#homeaddresspostalcode) 35

[HomeAddressState](#homeaddressstate) 36

[HomeAddressStreet](#homeaddressstreet) 36

[HomeFaxNumber](#homefaxnumber) 37

[HomePhoneNumber](#homephonenumber) 37

[IMAddress](#imaddress) 39

[IMAddress2](#imaddress2) 39

[IMAddress3](#imaddress3) 40

[JobTitle](#jobtitle) 40

[LastName](#lastname) 41

[ManagerName](#managername) 42

[MiddleName](#middlename) 42

[MMS](#mms) 43

[MobilePhoneNumber](#mobilephonenumber) 43

[NickName](#nickname) 44

[OfficeLocation](#officelocation) 45

[OtherAddressCity](#otheraddresscity) 45

[OtherAddressCountry](#otheraddresscountry) 46

[OtherAddressPostalCode](#otheraddresspostalcode) 46

[OtherAddressState](#otheraddressstate) 47

[OtherAddressStreet](#otheraddressstreet) 48

[PagerNumber](#pagernumber) 48

[Picture](#picture) 49

[RadioPhoneNumber](#radiophonenumber) 49

[Spouse](#spouse) 50

[Suffix](#suffix) 51

[Title](#title) 51

[WebPage](#webpage) 52

[WeightedRank](#weightedrank) 52

[YomiCompanyName](#yomicompanyname) 53

[YomiFirstName](#yomifirstname) 54

[YomiLastName](#yomilastname) 54

[Elements message](#elements) 10

[Examples](#protocol-examples) 61

F

[Fields - vendor-extensible](#vendor-extensible-fields) 9

[Full XML schema](#appendix-a-full-xml-schema) 64

[XML schema](#appendix-a-full-xml-schema) 64

G

[Glossary](#glossary) 7

I

[Implementer - security considerations](#security-considerations-for-implementers) 63

[Index of security parameters](#index-of-security-parameters) 63

[Informative references](#informative-references) 8

Initialization

[client](#initialization) 56

[server](#initialization-1) 59

[Introduction](#introduction) 7

M

Message processing

[client](#message-processing-events-and-sequencing-rules) 57

[server](#message-processing-events-and-sequencing-rules-1) 59

Messages

[Elements](#elements) 10

[Namespaces](#namespaces) 10

[syntax](#message-syntax) 10

[transport](#transport) 10

N

[Namespaces message](#namespaces) 10

[Normative references](#normative-references) 8

O

Other local events

[client](#other-local-events) 58

[server](#other-local-events-1) 60

[Overview (synopsis)](#overview) 8

P

[Parameters - security index](#index-of-security-parameters) 63

[Preconditions](#prerequisitespreconditions) 9

[Prerequisites](#prerequisitespreconditions) 9

[Product behavior](#appendix-b-product-behavior) 70

R

[References](#references) 8

[informative](#informative-references) 8

[normative](#normative-references) 8

[Relationship to other protocols](#relationship-to-other-protocols) 9

S

Security

[implementer considerations](#security-considerations-for-implementers) 63

[parameter index](#index-of-security-parameters) 63

Sequencing rules

[client](#message-processing-events-and-sequencing-rules) 57

[server](#message-processing-events-and-sequencing-rules-1) 59

Server

[abstract data model](#abstract-data-model-1) 58

[initialization](#initialization-1) 59

[message processing](#message-processing-events-and-sequencing-rules-1) 59

[other local events](#other-local-events-1) 60

[sequencing rules](#message-processing-events-and-sequencing-rules-1) 59

[timer events](#timer-events-1) 60

[timers](#timers-1) 58

[Standards assignments](#standards-assignments) 9

T

Timer events

[client](#timer-events) 58

[server](#timer-events-1) 60

Timers

[client](#timers) 56

[server](#timers-1) 58

[Tracking changes](#change-tracking) 71

[Transport](#transport) 10

V

[Vendor-extensible fields](#vendor-extensible-fields) 9

[Versioning](#versioning-and-capability-negotiation) 9

X

[XML schema](#appendix-a-full-xml-schema) 64
