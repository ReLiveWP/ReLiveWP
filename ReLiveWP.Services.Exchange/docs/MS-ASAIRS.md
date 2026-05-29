**\[MS-ASAIRS\]:**

**Exchange ActiveSync: AirSyncBase Namespace Protocol**

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
  12/3/2008    1.0                Major            Initial Release.

  3/4/2009     1.0.1              Editorial        Revised and edited technical content.

  4/10/2009    2.0                Major            Updated technical content and applicable product releases.

  7/15/2009    3.0                Major            Revised and edited for technical content.

  11/4/2009    3.1.0              Minor            Updated the technical content.

  2/10/2010    3.0.2              Editorial        Updated the technical content.

  5/5/2010     4.0.0              Major            Updated and revised the technical content.

  8/4/2010     5.0                Major            Significantly changed the technical content.

  11/3/2010    6.0                Major            Significantly changed the technical content.

  3/18/2011    7.0                Major            Significantly changed the technical content.

  8/5/2011     7.1                Minor            Clarified the meaning of the technical content.

  10/7/2011    7.2                Minor            Clarified the meaning of the technical content.

  1/20/2012    8.0                Major            Significantly changed the technical content.

  4/27/2012    8.1                Minor            Clarified the meaning of the technical content.

  7/16/2012    9.0                Major            Significantly changed the technical content.

  10/8/2012    10.0               Major            Significantly changed the technical content.

  2/11/2013    10.0               None             No changes to the meaning, language, or formatting of the technical content.

  7/26/2013    11.0               Major            Significantly changed the technical content.

  11/18/2013   11.0               None             No changes to the meaning, language, or formatting of the technical content.

  2/10/2014    11.0               None             No changes to the meaning, language, or formatting of the technical content.

  4/30/2014    12.0               Major            Significantly changed the technical content.

  7/31/2014    12.0               None             No changes to the meaning, language, or formatting of the technical content.

  10/30/2014   13.0               Major            Significantly changed the technical content.

  5/26/2015    14.0               Major            Significantly changed the technical content.

  6/30/2015    15.0               Major            Significantly changed the technical content.

  9/14/2015    16.0               Major            Significantly changed the technical content.

  6/9/2016     17.0               Major            Significantly changed the technical content.

  2/28/2017    18.0               Major            Significantly changed the technical content.

  4/18/2017    18.0               None             No changes to the meaning, language, or formatting of the technical content.

  7/24/2018    19.0               Major            Significantly changed the technical content.

  10/1/2018    20.0               Major            Significantly changed the technical content.

  12/11/2018   20.1               Minor            Clarified the meaning of the technical content.

  6/18/2019    20.2               Minor            Clarified the meaning of the technical content.

  11/16/2021   20.3               Minor            Clarified the meaning of the technical content.

  4/29/2022    21.0               Major            Significantly changed the technical content.

  5/20/2025    22.0               Major            Significantly changed the technical content.
  -------------------------------------------------------------------------------------------------------------------------------

Table of Contents

[1 Introduction [7](#introduction)](#introduction)

[1.1 Glossary [7](#glossary)](#glossary)

[1.2 References [8](#references)](#references)

[1.2.1 Normative References [8](#normative-references)](#normative-references)

[1.2.2 Informative References [9](#informative-references)](#informative-references)

[1.3 Overview [9](#overview)](#overview)

[1.4 Relationship to Other Protocols [9](#relationship-to-other-protocols)](#relationship-to-other-protocols)

[1.5 Prerequisites/Preconditions [10](#prerequisitespreconditions)](#prerequisitespreconditions)

[1.6 Applicability Statement [10](#applicability-statement)](#applicability-statement)

[1.7 Versioning and Capability Negotiation [10](#versioning-and-capability-negotiation)](#versioning-and-capability-negotiation)

[1.8 Vendor-Extensible Fields [10](#vendor-extensible-fields)](#vendor-extensible-fields)

[1.9 Standards Assignments [10](#standards-assignments)](#standards-assignments)

[2 Messages [11](#messages)](#messages)

[2.1 Transport [11](#transport)](#transport)

[2.2 Message Syntax [11](#message-syntax)](#message-syntax)

[2.2.1 Namespaces [11](#namespaces)](#namespaces)

[2.2.2 Elements [11](#elements)](#elements)

[2.2.2.1 Accuracy [13](#accuracy)](#accuracy)

[2.2.2.2 Add [14](#add)](#add)

[2.2.2.3 AllOrNone [15](#allornone)](#allornone)

[2.2.2.3.1 AllOrNone (BodyPartPreference) [15](#allornone-bodypartpreference)](#allornone-bodypartpreference)

[2.2.2.3.2 AllOrNone (BodyPreference) [16](#allornone-bodypreference)](#allornone-bodypreference)

[2.2.2.4 Altitude [17](#altitude)](#altitude)

[2.2.2.5 AltitudeAccuracy [17](#altitudeaccuracy)](#altitudeaccuracy)

[2.2.2.6 Annotation [18](#annotation)](#annotation)

[2.2.2.7 Attachment [19](#attachment)](#attachment)

[2.2.2.8 Attachments [20](#attachments)](#attachments)

[2.2.2.9 Body [20](#body)](#body)

[2.2.2.10 BodyPart [21](#bodypart)](#bodypart)

[2.2.2.11 BodyPartPreference [22](#bodypartpreference)](#bodypartpreference)

[2.2.2.12 BodyPreference [23](#bodypreference)](#bodypreference)

[2.2.2.13 City [24](#city)](#city)

[2.2.2.14 ClientId [25](#clientid)](#clientid)

[2.2.2.15 Content [25](#content)](#content)

[2.2.2.16 ContentId [26](#contentid)](#contentid)

[2.2.2.16.1 ContentId (Add) [26](#contentid-add)](#contentid-add)

[2.2.2.16.2 ContentId (Attachment) [27](#contentid-attachment)](#contentid-attachment)

[2.2.2.17 ContentLocation [27](#contentlocation)](#contentlocation)

[2.2.2.17.1 ContentLocation (Add) [27](#contentlocation-add)](#contentlocation-add)

[2.2.2.17.2 ContentLocation (Attachment) [28](#contentlocation-attachment)](#contentlocation-attachment)

[2.2.2.18 ContentType [28](#contenttype)](#contenttype)

[2.2.2.18.1 ContentType (Add) [29](#contenttype-add)](#contenttype-add)

[2.2.2.18.2 ContentType (Properties) [29](#contenttype-properties)](#contenttype-properties)

[2.2.2.19 Country [30](#country)](#country)

[2.2.2.20 Data [30](#data)](#data)

[2.2.2.20.1 Data (Body) [30](#data-body)](#data-body)

[2.2.2.20.2 Data (BodyPart) [31](#data-bodypart)](#data-bodypart)

[2.2.2.21 Delete [32](#delete)](#delete)

[2.2.2.22 DisplayName [32](#displayname)](#displayname)

[2.2.2.22.1 DisplayName (Add) [32](#displayname-add)](#displayname-add)

[2.2.2.22.2 DisplayName (Attachment) [33](#displayname-attachment)](#displayname-attachment)

[2.2.2.22.3 DisplayName (Location) [34](#displayname-location)](#displayname-location)

[2.2.2.23 EstimatedDataSize [34](#estimateddatasize)](#estimateddatasize)

[2.2.2.23.1 EstimatedDataSize (Attachment) [34](#estimateddatasize-attachment)](#estimateddatasize-attachment)

[2.2.2.23.2 EstimatedDataSize (Body) [35](#estimateddatasize-body)](#estimateddatasize-body)

[2.2.2.23.3 EstimatedDataSize (BodyPart) [36](#estimateddatasize-bodypart)](#estimateddatasize-bodypart)

[2.2.2.24 FileReference [36](#filereference)](#filereference)

[2.2.2.24.1 FileReference (Attachment) [36](#filereference-attachment)](#filereference-attachment)

[2.2.2.24.2 FileReference (Delete) [37](#filereference-delete)](#filereference-delete)

[2.2.2.24.3 FileReference (Fetch) [38](#filereference-fetch)](#filereference-fetch)

[2.2.2.25 InstanceId [38](#instanceid)](#instanceid)

[2.2.2.26 IsInline [39](#isinline)](#isinline)

[2.2.2.26.1 IsInline (Add) [39](#isinline-add)](#isinline-add)

[2.2.2.26.2 IsInline (Attachment) [40](#isinline-attachment)](#isinline-attachment)

[2.2.2.27 Latitude [40](#latitude)](#latitude)

[2.2.2.28 Location [41](#location)](#location)

[2.2.2.29 LocationUri [42](#locationuri)](#locationuri)

[2.2.2.30 Longitude [43](#longitude)](#longitude)

[2.2.2.31 Method [43](#method)](#method)

[2.2.2.31.1 Method (Add) [43](#method-add)](#method-add)

[2.2.2.31.2 Method (Attachment) [44](#method-attachment)](#method-attachment)

[2.2.2.32 NativeBodyType [45](#nativebodytype)](#nativebodytype)

[2.2.2.33 Part [46](#part)](#part)

[2.2.2.34 PostalCode [46](#postalcode)](#postalcode)

[2.2.2.35 Preview [47](#preview)](#preview)

[2.2.2.35.1 Preview (Body) [47](#preview-body)](#preview-body)

[2.2.2.35.2 Preview (BodyPart) [48](#preview-bodypart)](#preview-bodypart)

[2.2.2.35.3 Preview (BodyPartPreference) [48](#preview-bodypartpreference)](#preview-bodypartpreference)

[2.2.2.35.4 Preview (BodyPreference) [49](#preview-bodypreference)](#preview-bodypreference)

[2.2.2.36 State [50](#state)](#state)

[2.2.2.37 Status [50](#status)](#status)

[2.2.2.38 Street [51](#street)](#street)

[2.2.2.39 Truncated [51](#truncated)](#truncated)

[2.2.2.39.1 Truncated (Body) [52](#truncated-body)](#truncated-body)

[2.2.2.39.2 Truncated (BodyPart) [52](#truncated-bodypart)](#truncated-bodypart)

[2.2.2.40 TruncationSize [53](#truncationsize)](#truncationsize)

[2.2.2.40.1 TruncationSize (BodyPartPreference) [53](#truncationsize-bodypartpreference)](#truncationsize-bodypartpreference)

[2.2.2.40.2 TruncationSize (BodyPreference) [54](#truncationsize-bodypreference)](#truncationsize-bodypreference)

[2.2.2.41 Type [54](#type)](#type)

[2.2.2.41.1 Type (Body) [55](#type-body)](#type-body)

[2.2.2.41.2 Type (BodyPart) [55](#type-bodypart)](#type-bodypart)

[2.2.2.41.3 Type (BodyPartPreference) [56](#type-bodypartpreference)](#type-bodypartpreference)

[2.2.2.41.4 Type (BodyPreference) [56](#type-bodypreference)](#type-bodypreference)

[2.2.3 Groups [57](#groups)](#groups)

[2.2.3.1 TopLevelSchemaProps [57](#toplevelschemaprops)](#toplevelschemaprops)

[3 Protocol Details [59](#protocol-details)](#protocol-details)

[3.1 Client Details [59](#client-details)](#client-details)

[3.1.1 Abstract Data Model [59](#abstract-data-model)](#abstract-data-model)

[3.1.2 Timers [59](#timers)](#timers)

[3.1.3 Initialization [59](#initialization)](#initialization)

[3.1.4 Higher-Layer Triggered Events [59](#higher-layer-triggered-events)](#higher-layer-triggered-events)

[3.1.5 Message Processing Events and Sequencing Rules [59](#message-processing-events-and-sequencing-rules)](#message-processing-events-and-sequencing-rules)

[3.1.5.1 Commands [59](#commands)](#commands)

[3.1.5.1.1 ItemOperations [59](#itemoperations)](#itemoperations)

[3.1.5.1.2 MeetingResponse [60](#meetingresponse)](#meetingresponse)

[3.1.5.1.3 Search [60](#search)](#search)

[3.1.5.1.4 SmartForward [61](#smartforward)](#smartforward)

[3.1.5.1.5 Sync [61](#sync)](#sync)

[3.1.6 Timer Events [62](#timer-events)](#timer-events)

[3.1.7 Other Local Events [62](#other-local-events)](#other-local-events)

[3.2 Server Details [62](#server-details)](#server-details)

[3.2.1 Abstract Data Model [62](#abstract-data-model-1)](#abstract-data-model-1)

[3.2.2 Timers [62](#timers-1)](#timers-1)

[3.2.3 Initialization [63](#initialization-1)](#initialization-1)

[3.2.4 Higher-Layer Triggered Events [63](#higher-layer-triggered-events-1)](#higher-layer-triggered-events-1)

[3.2.5 Message Processing Events and Sequencing Rules [63](#message-processing-events-and-sequencing-rules-1)](#message-processing-events-and-sequencing-rules-1)

[3.2.5.1 Validating XML [63](#validating-xml)](#validating-xml)

[3.2.5.2 Commands [63](#commands-1)](#commands-1)

[3.2.5.2.1 ItemOperations [63](#itemoperations-1)](#itemoperations-1)

[3.2.5.2.2 Search [65](#search-1)](#search-1)

[3.2.5.2.3 Sync [66](#sync-1)](#sync-1)

[3.2.6 Timer Events [67](#timer-events-1)](#timer-events-1)

[3.2.7 Other Local Events [67](#other-local-events-1)](#other-local-events-1)

[4 Protocol Examples [68](#protocol-examples)](#protocol-examples)

[5 Security [69](#security)](#security)

[5.1 Security Considerations for Implementers [69](#security-considerations-for-implementers)](#security-considerations-for-implementers)

[5.2 Index of Security Parameters [69](#index-of-security-parameters)](#index-of-security-parameters)

[6 Appendix A: Full XML Schema [70](#appendix-a-full-xml-schema)](#appendix-a-full-xml-schema)

[7 Appendix B: Product Behavior [73](#appendix-b-product-behavior)](#appendix-b-product-behavior)

[8 Change Tracking [74](#change-tracking)](#change-tracking)

[9 Index [75](#index)](#index)

# Introduction

The Exchange ActiveSync: AirSyncBase Namespace Protocol describes the elements in the AirSyncBase namespace, which are used by the commands specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) to identify the size, type, and content of the data sent by and returned to the client. The AirSyncBase namespace contains elements used in both request and response command messages.

Sections 1.5, 1.8, 1.9, 2, and 3 of this specification are normative. All other sections and examples in this specification are informative.

## Glossary

This document uses the following terms:

> []{#gt_6ab4cacc-0e1a-4843-b9e5-4f1fee5a695a .anchor}**Attachment object**: A set of properties that represents a file, [**Message object**](#gt_b6c15d0c-d992-421d-ba96-99d3b63894cf), or structured storage that is attached to a Message object and is visible through the attachments table for a Message object.
>
> []{#gt_179b9392-9019-45a3-880b-26f6890522b7 .anchor}**base64 encoding**: A binary-to-text encoding scheme whereby an arbitrary sequence of bytes is converted to a sequence of printable ASCII characters, as described in [\[RFC4648\]](https://go.microsoft.com/fwlink/?LinkId=90487).
>
> []{#gt_7204b2ed-dcef-4434-be15-6451f92d03fb .anchor}**calendar**: A date range that shows availability, meetings, and appointments for one or more users or resources. See also Calendar object.
>
> []{#gt_549c4960-e8be-4c24-bc2b-b86530f1c1bf .anchor}**Hypertext Markup Language (HTML)**: An application of the Standard Generalized Markup Language (SGML) that uses tags to mark elements in a document, as described in [\[HTML\]](https://go.microsoft.com/fwlink/?LinkId=89880).
>
> []{#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd .anchor}**Hypertext Transfer Protocol (HTTP)**: An application-level protocol for distributed, collaborative, hypermedia information systems (text, graphic images, sound, video, and other multimedia files) on the World Wide Web.
>
> []{#gt_f8f4c2f5-c760-4abe-a9a1-573302980088 .anchor}**message body**: The main message text of an email message. A few properties of a [**Message object**](#gt_b6c15d0c-d992-421d-ba96-99d3b63894cf) represent its message body, with one property containing the text itself and others defining its code page and its relationship to alternative body formats.
>
> []{#gt_b6c15d0c-d992-421d-ba96-99d3b63894cf .anchor}**Message object**: A set of properties that represents an email message, appointment, contact, or other type of personal-information-management object. In addition to its own properties, a Message object contains recipient properties that represent the addressees to which it is addressed, and an attachments table that represents any files and other Message objects that are attached to it.
>
> []{#gt_1f032bde-d2f7-4fc8-87d0-090964e7b5a5 .anchor}**message part**: A [**message body**](#gt_f8f4c2f5-c760-4abe-a9a1-573302980088) with a string property that contains only the portion of an email message that is original to the message. It does not include any previous, quoted messages. If a message does not quote a previous message, the message part is identical to the message body.
>
> []{#gt_fda94a53-448d-48d5-9991-176c530ff597 .anchor}**message store**: A unit of containment for a single hierarchy of Folder objects, such as a mailbox or public folders.
>
> []{#gt_af6ba277-34c1-493d-8103-71d2af36ce30 .anchor}**Multipurpose Internet Mail Extensions (MIME)**: A set of extensions that redefines and expands support for various types of content in email messages, as described in [\[RFC2045\]](https://go.microsoft.com/fwlink/?LinkId=90307), [\[RFC2046\]](https://go.microsoft.com/fwlink/?LinkId=90308), and [\[RFC2047\]](https://go.microsoft.com/fwlink/?LinkId=90309).
>
> []{#gt_171744b8-3f44-4198-b7b9-1c0147282d2c .anchor}**Object Linking and Embedding (OLE)**: A technology for transferring and sharing information between applications by inserting a file or part of a file into a compound document. The inserted file can be either embedded or linked. See also embedded object and linked object.
>
> []{#gt_0efee4a8-a2e9-48fe-87f8-d45097de6b72 .anchor}**orphan instance**: An instance of an event that is in a [**recurring series**](#gt_2325d666-e02f-49e4-afa5-3e896d672efe) and is in a Calendar folder without the recurring series. For all practical purposes, this is a single instance.
>
> []{#gt_afa1b8ad-29c4-4f4a-90ce-e63b3547e15a .anchor}**plain text**: Text that does not have markup. See also plain text message body.
>
> []{#gt_2325d666-e02f-49e4-afa5-3e896d672efe .anchor}**recurring series**: An event that repeats at specific intervals of time according to a recurrence pattern.
>
> []{#gt_a9aa8673-7798-4eba-a048-8b7c95a7b080 .anchor}**Rich Text Format (RTF)**: Text with formatting as described in [\[MSFT-RTF\]](https://go.microsoft.com/fwlink/?LinkId=120924).
>
> []{#gt_c305d0ab-8b94-461a-bd76-13b40cb8c4d8 .anchor}**Unicode**: A character encoding standard developed by the Unicode Consortium that represents almost all of the written languages of the world. The [**Unicode**](#gt_c305d0ab-8b94-461a-bd76-13b40cb8c4d8) standard [\[UNICODE5.0.0/2007\]](https://go.microsoft.com/fwlink/?LinkId=154659) provides three forms (UTF-8, UTF-16, and UTF-32) and seven schemes (UTF-8, UTF-16, UTF-16 BE, UTF-16 LE, UTF-32, UTF-32 LE, and UTF-32 BE).
>
> []{#gt_e18af8e8-01d7-4f91-8a1e-0fb21b191f95 .anchor}**Uniform Resource Identifier (URI)**: A string that identifies a resource. The URI is an addressing mechanism defined in Internet Engineering Task Force (IETF) Uniform Resource Identifier (URI): Generic Syntax [\[RFC3986\]](https://go.microsoft.com/fwlink/?LinkId=90453).
>
> []{#gt_485f05b3-df3b-45ac-b8bf-d05f5d185a24 .anchor}**XML namespace**: A collection of names that is used to identify elements, types, and attributes in XML documents identified in a URI reference \[RFC3986\]. A combination of XML namespace and local name allows XML documents to use elements, types, and attributes that have the same names but come from different sources. For more information, see [\[XMLNS-2ED\]](https://go.microsoft.com/fwlink/?LinkId=90602).
>
> []{#gt_bd0ce6f9-c350-4900-827e-951265294067 .anchor}**XML schema**: A description of a type of XML document that is typically expressed in terms of constraints on the structure and content of documents of that type, in addition to the basic syntax constraints that are imposed by XML itself. An XML schema provides a view of a document type at a relatively high level of abstraction.
>
> **MAY, SHOULD, MUST, SHOULD NOT, MUST NOT:** These terms (in all caps) are used as defined in [\[RFC2119\]](https://go.microsoft.com/fwlink/?LinkId=90317). All statements of optional behavior use either MAY, SHOULD, or SHOULD NOT.

## References

Links to a document in the Microsoft Open Specifications library point to the correct section in the most recently published version of the referenced document. However, because individual documents in the library are not updated at the same time, the section numbers in the documents may not match. You can confirm the correct section numbering by checking the [Errata](https://go.microsoft.com/fwlink/?linkid=850906).

### Normative References

We conduct frequent surveys of the normative references to assure their continued availability. If you have any issue with finding a normative reference, please contact <dochelp@microsoft.com>. We will assist you in finding the relevant information.

\[MS-ASCAL\] Microsoft Corporation, \"[Exchange ActiveSync: Calendar Class Protocol](%5bMS-ASCAL%5d.pdf#Section_0c4486824a6a459aae662fed0712bef9)\".

\[MS-ASCMD\] Microsoft Corporation, \"[Exchange ActiveSync: Command Reference Protocol](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a)\".

\[MS-ASCNTC\] Microsoft Corporation, \"[Exchange ActiveSync: Contact Class Protocol](%5bMS-ASCNTC%5d.pdf#Section_a4593b9dd9af4d27bc5c67c4c1b98d54)\".

\[MS-ASDTYPE\] Microsoft Corporation, \"[Exchange ActiveSync: Data Types](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3)\".

\[MS-ASEMAIL\] Microsoft Corporation, \"[Exchange ActiveSync: Email Class Protocol](%5bMS-ASEMAIL%5d.pdf#Section_f3d27369e0f54164aa5e9b1abda16f5f)\".

\[MS-ASHTTP\] Microsoft Corporation, \"[Exchange ActiveSync: HTTP Protocol](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d)\".

\[MS-ASTASK\] Microsoft Corporation, \"[Exchange ActiveSync: Tasks Class Protocol](%5bMS-ASTASK%5d.pdf#Section_b8fe266450ba4d00bf6be4deab352c89)\".

\[RFC2119\] Bradner, S., \"Key words for use in RFCs to Indicate Requirement Levels\", BCP 14, RFC 2119, March 1997, [https://www.rfc-editor.org/info/rfc2119](https://go.microsoft.com/fwlink/?LinkId=90317)

\[XMLNS\] Bray, T., Hollander, D., Layman, A., et al., Eds., \"Namespaces in XML 1.0 (Third Edition)\", W3C Recommendation, December 2009, [https://www.w3.org/TR/2009/REC-xml-names-20091208/](https://go.microsoft.com/fwlink/?LinkId=191840)

\[XMLSCHEMA1/2\] Thompson, H., Beech, D., Maloney, M., and Mendelsohn, N., Eds., \"XML Schema Part 1: Structures Second Edition\", W3C Recommendation, October 2004, [https://www.w3.org/TR/2004/REC-xmlschema-1-20041028/](https://go.microsoft.com/fwlink/?LinkId=90607)

### Informative References

\[MS-ASCON\] Microsoft Corporation, \"[Exchange ActiveSync: Conversations Protocol](%5bMS-ASCON%5d.pdf#Section_8571bf985f7b4c2fab28c32176d20169)\".

\[MS-ASDOC\] Microsoft Corporation, \"[Exchange ActiveSync: Document Class Protocol](%5bMS-ASDOC%5d.pdf#Section_c503701c0e594beb9b8b038cd69a3443)\".

\[MS-ASMS\] Microsoft Corporation, \"[Exchange ActiveSync: Short Message Service (SMS) Protocol](%5bMS-ASMS%5d.pdf#Section_3123f34aaabe4ec5aa836f6d48698a8b)\".

\[MS-ASNOTE\] Microsoft Corporation, \"[Exchange ActiveSync: Notes Class Protocol](%5bMS-ASNOTE%5d.pdf#Section_21801d6c000e413c859150430a8e9fd9)\".

\[MS-ASRM\] Microsoft Corporation, \"[Exchange ActiveSync: Rights Management Protocol](%5bMS-ASRM%5d.pdf#Section_71e681b7e1784c1096b678df7fa77dfc)\".

\[MS-OXPROTO\] Microsoft Corporation, \"[Exchange Server Protocols System Overview](%5bMS-OXPROTO%5d.pdf#Section_734ab967e43e425babe1974af56c0283)\".

\[NGA-WGS84\] NGA, \"Department of Defense (DoD) World Geodetic System (WGS) 1984 - Its Definition and Relationships with Local Geodetic Systems\", NGA.STND.0036_1.0.0_WGS84, Version 1.0.0, July 2014, [http://earth-info.nga.mil/GandG/publications/NGA_STND_0036_1_0_0_WGS84/NGA.STND.0036_1.0.0_WGS84.pdf](https://go.microsoft.com/fwlink/?LinkId=616320)

## Overview

The elements specified in the AirSyncBase namespace are used by multiple ActiveSync commands to identify the size, type, and content of data sent by and returned to the client. In order to use the elements in the AirSyncBase namespace, the namespace and elements are included in the command request and response messages as specified in this document.

## Relationship to Other Protocols

The AirSyncBase namespace is used by the following protocols.

-   Exchange ActiveSync: Calendar Class Protocol, described in [\[MS-ASCAL\]](%5bMS-ASCAL%5d.pdf#Section_0c4486824a6a459aae662fed0712bef9)

-   Exchange ActiveSync: Command Reference Protocol, described in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a)

-   Exchange ActiveSync: Contact Class Protocol, described in [\[MS-ASCNTC\]](%5bMS-ASCNTC%5d.pdf#Section_a4593b9dd9af4d27bc5c67c4c1b98d54)

-   Exchange ActiveSync: Conversations Protocol, described in [\[MS-ASCON\]](%5bMS-ASCON%5d.pdf#Section_8571bf985f7b4c2fab28c32176d20169)

-   Exchange ActiveSync: Document Class Protocol, described in [\[MS-ASDOC\]](%5bMS-ASDOC%5d.pdf#Section_c503701c0e594beb9b8b038cd69a3443)

-   Exchange ActiveSync: Email Class Protocol, described in [\[MS-ASEMAIL\]](%5bMS-ASEMAIL%5d.pdf#Section_f3d27369e0f54164aa5e9b1abda16f5f)

-   Exchange ActiveSync: Short Message Service (SMS) Protocol, described in [\[MS-ASMS\]](%5bMS-ASMS%5d.pdf#Section_3123f34aaabe4ec5aa836f6d48698a8b)

-   Exchange ActiveSync: Notes Class Protocol, described in [\[MS-ASNOTE\]](%5bMS-ASNOTE%5d.pdf#Section_21801d6c000e413c859150430a8e9fd9)

-   Exchange ActiveSync: Rights Management Protocol, described in [\[MS-ASRM\]](%5bMS-ASRM%5d.pdf#Section_71e681b7e1784c1096b678df7fa77dfc)

-   Exchange ActiveSync: Tasks Class Protocol, described in [\[MS-ASTASK\]](%5bMS-ASTASK%5d.pdf#Section_b8fe266450ba4d00bf6be4deab352c89)

The elements in this specification use data types specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3).

For conceptual background information and overviews of the relationships and interactions between this and other protocols, see [\[MS-OXPROTO\]](%5bMS-OXPROTO%5d.pdf#Section_734ab967e43e425babe1974af56c0283).

## Prerequisites/Preconditions

To use the elements in the AirSyncBase namespace, include the namespace in the command request. The namespace is included by adding the following to the command request:

\<CommandName xmlns:airsyncbase=\"ClassName:\"\>

For a complete example, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 4.10.1.1.

## Applicability Statement

This specification applies to the **ItemOperations**, **MeetingResponse**, **Search**, **SmartForward** and **Sync** commands, as specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

## Versioning and Capability Negotiation

None.

## Vendor-Extensible Fields

The **Type** element can be extended to include custom message types. For more details, see section [2.2.2.41](#Section_595f22c0e49844db9c6cf6a914047cee).

## Standards Assignments

None.

# Messages

## Transport

The elements specified in the following sections are sent and received by using the **ItemOperations**, **MeetingResponse**, **Search**, **SmartForward**, and **Sync** commands, as specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

## Message Syntax

The [**XML schema**](#gt_bd0ce6f9-c350-4900-827e-951265294067) for the **AirSyncBase** namespace is described in section [6](#Section_fbbc1af95a714925aca860a95339c722).

### Namespaces

This specification defines and references various [**XML namespaces**](#gt_485f05b3-df3b-45ac-b8bf-d05f5d185a24) using the mechanisms specified in [\[XMLNS\]](https://go.microsoft.com/fwlink/?LinkId=191840). Although this specification associates a specific XML namespace prefix for each XML namespace that is used, the choice of any particular XML namespace prefix is implementation-specific and not significant for interoperability.

  -------------------------------------------------------------------------------------------------------------------------------------------------------
  Prefix            Namespace URI                          Reference
  ----------------- -------------------------------------- ----------------------------------------------------------------------------------------------
  None              **AirSyncBase**                        

  airsync           **AirSync**                            [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21

  calendar          **Calendar**                           [\[MS-ASCAL\]](%5bMS-ASCAL%5d.pdf#Section_0c4486824a6a459aae662fed0712bef9) section 2.2

  email             **Email**                              [\[MS-ASEMAIL\]](%5bMS-ASEMAIL%5d.pdf#Section_f3d27369e0f54164aa5e9b1abda16f5f) section 2.2

  email2            **Email2**                             \[MS-ASEMAIL\] section 2.2

  itemoperations    **ItemOperations**                     \[MS-ASCMD\] section 2.2.1.10

  meetingresponse   **MeetingResponse**                    \[MS-ASCMD\] section 2.2.1.11

  search            **Search**                             \[MS-ASCMD\] section 2.2.1.16

  composemail       **ComposeMail**                        \[MS-ASCMD\] section 2.2.1.19

  xs                **http://www.w3.org/2001/XMLSchema**   [\[XMLSCHEMA1/2\]](https://go.microsoft.com/fwlink/?LinkId=90607)
  -------------------------------------------------------------------------------------------------------------------------------------------------------

### Elements

The following table summarizes the set of common [**XML schema**](#gt_bd0ce6f9-c350-4900-827e-951265294067) element definitions defined by this specification. XML schema element definitions that are specific to a particular operation are described with the operation.

  --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Element name                                                                             Description
  ---------------------------------------------------------------------------------------- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  **Accuracy** (section [2.2.2.1](#Section_13318ff160fe435c81d86a0a6ac92863))              Specifies the accuracy of the values of the **Latitude** and **Longitude** elements.

  **Add** (section [2.2.2.2](#Section_d60d864c07304b20bd74cc0245d4cdc6))                   Adds an attachment to a [**calendar**](#gt_7204b2ed-dcef-4434-be15-6451f92d03fb) item or to a draft email item.

  **AllOrNone** (section [2.2.2.3](#Section_cce4e3bdf1ab4a5484bbfa00a835da0b))             Specifies whether to search, synchronize, or retrieve all or none of the content based on the **TruncationSize** element.

  **Altitude** (section [2.2.2.4](#Section_cd8db29335c8457b867cdb2ba10cda59))              Specifies the the altitude of an event\'s location.

  **AltitudeAccuracy** (section [2.2.2.5](#Section_6b87364e7cda485691298775159a61aa))      Specifies the accuracy of the value of the **Altitude** element.

  **Annotation** (section [2.2.2.6](#Section_68b6b09643a745cf9f61492334efaced))            Specifies a note about the location of an event.

  **Attachment** (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda))            Specifies the attachment information for a single attachment item.

  **Attachments** (section [2.2.2.8](#Section_12f116075694421ab7d1e436da4da613))           Contains a collection of elements that specify one or more attachment items.

  **Body** (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de))                  Contains a collection of elements that specify a free-form, variable-length data field associated with a stored item on the server.

  **BodyPart** (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5))             Contains a collection of elements that specify the [**message part**](#gt_1f032bde-d2f7-4fc8-87d0-090964e7b5a5) of the body of an e-mail.

  **BodyPartPreference** (section [2.2.2.11](#Section_6d9d09fd2ebc4570ba5cd6af546b3b12))   Contains a collection of elements that set the preference information related to the type and size of information that is returned from searching, synchronizing, or fetching a **BodyPart**.

  **BodyPreference** (section [2.2.2.12](#Section_793666561b7e4564a930ffb6fa85d283))       Contains a collection of elements that set the preference information related to the type and size of information that is returned from searching, synchronizing, or fetching.

  **City** (section [2.2.2.13](#Section_00241f454bbf461880f04e74584121e0))                 Specifies the city in which an event occurs.

  **ClientId** (section [2.2.2.14](#Section_760294dc8295419eace3700bfa7c7266))             Specifies a client-generated temporary identifier that links to the file that is being added as an attachment.

  **Content** (section [2.2.2.15](#Section_81bede41154743ef89f0d85e0e0ed2c1))              Contains the content of the attachment that is being added.

  **ContentId** (section [2.2.2.16](#Section_ba04ee638e2e4e70827876835a14ca55))            Contains an attachment\'s unique object that is used to reference the attachment within the item to which the attachment belongs.

  **ContentLocation** (section [2.2.2.17](#Section_e97b0503877d48d09b55f8912716b5a0))      Contains an attachment\'s relative [**URI**](#gt_e18af8e8-01d7-4f91-8a1e-0fb21b191f95), which is used to associate the attachment in other items.

  **ContentType** (section [2.2.2.18](#Section_b8393dba260740fca7243abb26c1fa15))          Specifies the type of data that is contained either in the **Content** element or in the **itemoperation:Data** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.39.2).

  **Country** (section [2.2.2.19](#Section_41cd8a0438b1493bb0bdb600429bc531))              Specifies the country in which an event occurs.

  **Data** (section [2.2.2.20](#Section_7559b551b545499b91d9719f2e8a5fc9))                 Specifies the data associated with an item\'s **Body** element or **BodyPart** element.

  **Delete** (section [2.2.2.21](#Section_b9f6e6cdfe714014a148cb21694f9a80))               Deletes an attachment from a calendar item or from a draft email item.

  **DisplayName** (section [2.2.2.22](#Section_f1327c9389814fc69f3651aed748a76b))          Specifies the display name of an attachment or the display name of an event\'s location.

  **EstimatedDataSize** (section [2.2.2.23](#Section_9bc1c02398ba4845a270534fa051ce70))    Specifies an informational estimate of the size of the data associated with an item\'s **Body** element, **BodyPart** element, or **Attachment** element.

  **FileReference** (section [2.2.2.24](#Section_4d364b1241684d77b34d5313fb535243))        Specifies the server-assigned unique identifier of an attachment.

  **InstanceId** (section [2.2.2.25](#Section_f684d282fb7e4326b64c1b8f68afc2ed))           Specifies the original, unmodified, UTC date and time of a particular instance of a [**recurring series**](#gt_2325d666-e02f-49e4-afa5-3e896d672efe).

  **IsInline** (section [2.2.2.26](#Section_a2a7a92871524c679cf82b1ff7059a6b))             Specifies whether the attachment is embedded in the message.

  **Latitude** (section [2.2.2.27](#Section_3c07ac1c46a44b89a5eff5abc30b297c))             Specifies the latitude of the event\'s location.

  **Location** (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0))             Specifies details about the location of an event.

  **LocationUri** (section [2.2.2.29](#Section_8256aa9c53b4427f9f95d3ae17f38c02))          Specifies the URI for the location of an event.

  **Longitude** (section [2.2.2.30](#Section_7f62f1e593be4675b389b63903c5d472))            Specifies the longitude of the event\'s location.

  **Method** (section [2.2.2.31](#Section_949ae160e1614399a706b8953cb44c1e))               Identifies the method in which the attachment was attached.

  **NativeBodyType** (section [2.2.2.32](#Section_7a91ce24d4d1471c84723da9a59ebe1c))       Specifies the original format type of the item.

  **Part** (section [2.2.2.33](#Section_2e08aa0ad12e4b9ab3979e32ef4186b5))                 Specifies the integer index into the metadata of the multipart response.

  **PostalCode** (section [2.2.2.34](#Section_bfa47960d8844de299a42023647f5b41))           Specifies the postal code for the address of the event\'s location.

  **Preview** (section [2.2.2.35](#Section_7dc79a2eef734ecd8100fd8cf00a28fa))              Specifies the message preview or the maximum length of the message preview to be returned to the client.

  **State** (section [2.2.2.36](#Section_efbda0bb756f4ecdace411bc5477dec0))                Specifies the state or province in which an event occurs.

  **Status** (section [2.2.2.37](#Section_e1ec362012314d11b57752bb6a1d77be))               Specifies the status of the **Data** element within the **BodyPart** response.

  **Street** (section [2.2.2.38](#Section_bbf2888e23344087afa54551e2d400ce))               Specifies the street address of the event\'s location.

  **Truncated** (section [2.2.2.39](#Section_a06dc8896df549f499e3ae88d7eda520))            Specifies whether the body or body part of the item has been truncated according to the **BodyPreference** element or the **BodyPartPreference** element.

  **TruncationSize** (section [2.2.2.40](#Section_cc29d2e4d62d400bb7f37408ee0bdc1d))       Specifies the size, in bytes, of the content that the client wants to search, synchronize, or fetch.

  **Type** (section [2.2.2.41](#Section_595f22c0e49844db9c6cf6a914047cee))                 Specifies the format type of the body content of the item.
  --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

#### Accuracy

The **Accuracy** element is an optional child element of the **Location** element (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0)) that specifies the accuracy of the values of the **Latitude** element (section [2.2.2.27](#Section_3c07ac1c46a44b89a5eff5abc30b297c)) and the **Longitude** element (section [2.2.2.30](#Section_7f62f1e593be4675b389b63903c5d472)).

The **Accuracy** element is a **double** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.4.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Add

The **Add** element is an optional child element of the **Attachments** element (section [2.2.2.8](#Section_12f116075694421ab7d1e436da4da613)) that adds an attachment to a [**calendar**](#gt_7204b2ed-dcef-4434-be15-6451f92d03fb) item or to a draft email item.

The **Add** element is a **container** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2. It has the following child elements:

-   **ClientId** (section [2.2.2.14](#Section_760294dc8295419eace3700bfa7c7266)) --- This element is required.

-   **Content** (section [2.2.2.15](#Section_81bede41154743ef89f0d85e0e0ed2c1)) --- This element is required.

-   **ContentId** (section [2.2.2.16.1](#Section_8a4746a7756a414687d1270d1c3e2f4f)) --- This element is optional.

-   **ContentLocation** (section [2.2.2.17.1](#Section_0bf6e69606bc423f96f4e80a6ad1f438)) --- This element is optional.

-   **ContentType** (section [2.2.2.18.1](#Section_5c9b829b1de9400086382525d17dd850)) --- This element is optional.

-   **DisplayName** (section [2.2.2.22.1](#Section_2ddce3aaee104aacb717cd3ec6506704)) --- This element is required.

-   **IsInline** (section [2.2.2.26.1](#Section_b427fb12e516406399c0c3f1a3a2cdcd)) --- This element is optional.

-   **Method** (section [2.2.2.31.1](#Section_ce0e5014f5b247f8ba377983dbb2a8d0)) --- This element is required.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### AllOrNone

The **AllOrNone** element is a child element of the **BodyPartPreference** element (section [2.2.2.11](#Section_6d9d09fd2ebc4570ba5cd6af546b3b12)) and the **BodyPreference** element (section [2.2.2.12](#Section_793666561b7e4564a930ffb6fa85d283)) that specifies whether to search, synchronize, or retrieve all or none of the content based on the **TruncationSize** element (section [2.2.2.40](#Section_cc29d2e4d62d400bb7f37408ee0bdc1d)).

The value of this element is a **boolean** value ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.1). When the value is set to 1 (TRUE) and the content has not been truncated, all of the content is searched, synchronized, or retrieved. When the value is set to 1 (TRUE) and the content has been truncated, the content is not searched, synchronized, or retrieved. When the value is set to 0 (FALSE), the truncated or nontruncated content is searched, synchronized, or retrieved.

##### AllOrNone (BodyPartPreference)

The **AllOrNone** element is an optional child element of the **BodyPartPreference** element (section [2.2.2.11](#Section_6d9d09fd2ebc4570ba5cd6af546b3b12)). A command request MUST have a maximum of 1 **AllOrNone** element per **BodyPartPreference** element. If the **AllOrNone** element is not included in the request, the truncated or nontruncated content is searched, synchronized, or retrieved as if the value was set to 0 (FALSE). The **AllOrNone** element MUST NOT be used in command responses.

This element MUST be ignored if the **TruncationSize** element is not included.

A client can include multiple **BodyPartPreference** elements in a command request with different values for the **Type** element (section [2.2.2.41.3](#Section_7044e1712c5945e98de77ba8b74e1f3f)). By default, the server returns the data truncated to the size requested by **TruncationSize** for the **Type** element that matches the native storage format of the item\'s **Body** element (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de)). But, if the client also includes the **AllOrNone** element with a value of 1 (TRUE) along with the **TruncationSize** element, it is instructing the server not to return a truncated response for that type when the size (in bytes) of the available data exceeds the value of the **TruncationSize** element. For example, a client can use these two elements to signify that it cannot process partial [**Rich Text Format (RTF)**](#gt_a9aa8673-7798-4eba-a048-8b7c95a7b080) data (a **Type** element value of 3). In this case, if the client has specified multiple **BodyPartPreference** elements, the server selects the next **BodyPartPreference** element that will return the maximum amount of body text to the client. Assume that the client specifies two **BodyPartPreference** elements:

1.  \<airsyncbase:BodyPartPreference\>

    \<airsyncbase:Type\>2\</airsyncbase:Type\>

    \<airsyncbase:AllOrNone\>1\</airsyncbase:AllOrNone\>

    \<airsyncbase:TruncationSize\>50\</airsyncbase:TruncationSize\>

    \</airsyncbase:BodyPartPreference\>

    \<airsyncbase:BodyPartPreference\>

    \<airsyncbase:Type\>1\</airsyncbase:Type\>

    \<airsyncbase:TruncationSize\>50\</airsyncbase:TruncationSize\>

    \</airsyncbase:BodyPartPreference\>

The first **BodyPartPreference** element requests an [**HTML**](#gt_549c4960-e8be-4c24-bc2b-b86530f1c1bf) body, but only if the body size is less than 50 bytes. The second requests an element in [**plain text**](#gt_afa1b8ad-29c4-4f4a-90ce-e63b3547e15a) format. If the client requests a text body whose native format is HTML, and the size of the data exceeds 50 bytes, the server converts the body to plain text and returns the first 50 bytes of plain text data.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

##### AllOrNone (BodyPreference)

The **AllOrNone** element is an optional child element of the **BodyPreference** element (section [2.2.2.12](#Section_793666561b7e4564a930ffb6fa85d283)). A command request MUST have a maximum of 1 **AllOrNone** element per **BodyPreference** element. If the **AllOrNone** element is not included in the request, then the truncated or non-truncated content is searched, synchronized, or retrieved as if the value was set to 0 (FALSE). The **AllOrNone** element MUST NOT be used in command responses.

This element MUST be ignored if the **TruncationSize** element is not included.

A client can include multiple **BodyPreference** elements in a command request with different values for the **Type** element (section [2.2.2.41.4](#Section_5a816c90c5fe4b4299ac5f85c7f3efb6)). By default, the server returns the data truncated to the size requested by **TruncationSize** for the **Type** element that matches the native storage format of the item\'s **Body** element (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de)). But, if the client also includes the **AllOrNone** element with a value of 1 (TRUE) along with the **TruncationSize** element, it is instructing the server not to return a truncated response for that type when the size (in bytes) of the available data exceeds the value of the **TruncationSize** element. For example, a client can use these two elements to signify that it cannot process partial [**Rich Text Format (RTF)**](#gt_a9aa8673-7798-4eba-a048-8b7c95a7b080) data (a **Type** element value of 3). In this case, if the client has specified multiple **BodyPreference** elements, the server selects the next **BodyPreference** element that will return the maximum amount of body text to the client. Assume that the client specifies two **BodyPreference** elements.

11. \<airsyncbase:BodyPreference\>

    \<airsyncbase:Type\>2\</airsyncbase:Type\>

    \<airsyncbase:AllOrNone\>1\</airsyncbase:AllOrNone\>

    \<airsyncbase:TruncationSize\>50\</airsyncbase:TruncationSize\>

    \</airsyncbase:BodyPreference\>

    \<airsyncbase:BodyPreference\>

    \<airsyncbase:Type\>1\</airsyncbase:Type\>

    \<airsyncbase:TruncationSize\>50\</airsyncbase:TruncationSize\>

    \</airsyncbase:BodyPreference\>

The first **BodyPreference** element requests an [**HTML**](#gt_549c4960-e8be-4c24-bc2b-b86530f1c1bf) body, but only if the body size is less than 50 bytes. The second requests an element in [**plain text**](#gt_afa1b8ad-29c4-4f4a-90ce-e63b3547e15a) format. If the client requests a text body whose native format is HTML, and the size of the data exceeds 50 bytes, the server converts the body to plain text and returns the first 50 bytes of plain text data.

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

#### Altitude

The **Altitude** element is an optional child element of the **Location** element (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0)) that specifies the altitude of an event\'s location. The altitude is measured in meters above the WGS-84 ellipsoid, which is described in [\[NGA-WGS84\]](https://go.microsoft.com/fwlink/?LinkId=616320).

The **Altitude** element is a **double** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.4.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### AltitudeAccuracy

The **AltitudeAccuracy** element is an optional child element of the **Location** element (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0)) that specifies the accuracy of the value of the **Altitude** element (section [2.2.2.4](#Section_cd8db29335c8457b867cdb2ba10cda59)).

The **AltitudeAccuracy** element is a **double** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.4.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Annotation

The **Annotation** element is an optional child element of the **Location** element (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0)) that specifies a note about the location of an event.

The **Annotation** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Attachment

The **Attachment** element is a required child element of the **Attachments** element (section [2.2.2.8](#Section_12f116075694421ab7d1e436da4da613)) and specifies the attachment information for a single attachment item.

Command requests MUST NOT include the **Attachment** element.

The **Attachment** element is a **container** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2.

The **Attachment** element has the following child elements, in any order, in a **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) with a **Commands** element (\[MS-ASCMD\] section 2.2.3.32), an **ItemOperations** command response (\[MS-ASCMD\] section 2.2.1.10), or a **Search** command response (\[MS-ASCMD\] section 2.2.1.16):

-   **DisplayName** (section [2.2.2.22.2](#Section_b9ac5dbd51a6489c825d2c9f3c9bf41c)). This element is optional.

-   **FileReference** (section [2.2.2.24.1](#Section_9f86ef66b7ed48ad909f15c0e338fac8)). This element is required.

-   **Method** (section [2.2.2.31.2](#Section_a1408a2d517848958103e66b435432be)). This element is required.

-   **EstimatedDataSize** (section [2.2.2.23.1](#Section_6d5635c427da46c6b31dd4caea1df7f9)). This element is required.

-   **ContentId** (section [2.2.2.16.2](#Section_be51de6eeb8341fbb79ca02d762c2ed0)). This element is optional.

-   **ContentLocation** (section [2.2.2.17.2](#Section_fe8bead0e57c4948ad5d68867e4c3f76)). This element is optional.

-   **IsInline** (section [2.2.2.26.2](#Section_c7e6d9c8b69a4e0c95769e55de793b65)). This element is optional.

-   **email2:UmAttDuration** ([\[MS-ASEMAIL\]](%5bMS-ASEMAIL%5d.pdf#Section_f3d27369e0f54164aa5e9b1abda16f5f) section 2.2.2.81). This element is optional.

-   **email2:UmAttOrder** (\[MS-ASEMAIL\] section 2.2.2.82). This element is optional.

The **Attachment** element has the following child elements, in any order, in a **Sync** command response with a **Responses** element (\[MS-ASCMD\] section 2.2.3.154):

-   **ClientId** (section [2.2.2.14](#Section_760294dc8295419eace3700bfa7c7266)). This element is required.

-   **FileReference** (section 2.2.2.24.1). This element is required.

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

The server returns the **Attachment** element in a **Sync** command response with a **Responses** element only when protocol version 16.0 or 16.1 is used.

#### Attachments

The **Attachments** element is an optional child element of the **airsync:ApplicationData** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.11), the **itemoperations:Properties** element (\[MS-ASCMD\] section 2.2.3.139.2), and the **search:Properties** element (\[MS-ASCMD\] section 2.2.3.139.3) that contains one or more attachment items.

The **Attachments** element is a **container** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2. It has the following child elements:

-   **Attachment** (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda)), in a **Sync** (\[MS-ASCMD\] section 2.2.1.21), **ItemOperations** (\[MS-ASCMD\] section 2.2.1.10), or **Search** (\[MS-ASCMD\] section 2.2.1.16) command response

-   **Add** (section [2.2.2.2](#Section_d60d864c07304b20bd74cc0245d4cdc6)), in a **Sync** command request

-   **Delete** (section [2.2.2.21](#Section_b9f6e6cdfe714014a148cb21694f9a80)), in a **Sync** command request

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

The **email:Attachments** element, as specified in [\[MS-ASEMAIL\]](%5bMS-ASEMAIL%5d.pdf#Section_f3d27369e0f54164aa5e9b1abda16f5f) section 2.2.2.4.2, is used with protocol version 2.5 instead of the **Attachments** element of the **AirSyncBase** namespace.

#### Body

The **Body** element is an optional child element of the **airsync:ApplicationData** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.11), the **itemoperations:Properties** element (\[MS-ASCMD\] section 2.2.3.139.2), the **search:Properties** element (\[MS-ASCMD\] section 2.2.3.139.3), the **meetingresponse:SendResponse** element (\[MS-ASCMD\] section 2.2.3.163), and the **composemail:SmartForward** element (\[MS-ASCMD\] section 2.2.3.169) that specifies a free-form, variable-length data field associated with an item stored on the server. The item can be for any of the following content classes: **Calendar**, **Contact**, **Email**, **Notes**, **SMS**, or **Tasks**.

The **Body** element is a **container** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2.

The **Body** element MUST be included in a response message whenever an item has changes or new items are created. There is no limit on the number of **Body** elements in a command response. When included in a command response, the **Body** element indicates the existence of one or more variable-length fields of data associated with the item. Command requests can include the **Body** element.

The **Body** element, if present, has the following required and optional child elements in this order:

-   **Type** (section [2.2.2.41.1](#Section_02747a2d938143f499a9e854c6422901)): This element is required.

-   **EstimatedDataSize** (section [2.2.2.23.2](#Section_108e3cbcc8b94b77a088e88c7bb4a8aa)): This element is optional.

-   **Truncated** (section [2.2.2.39.1](#Section_cada9eae58664657b11ea0feac6804e7)). This element is optional.

-   **Data** (section [2.2.2.20.1](#Section_b673d5ba6afe466d97722a0ceaa49c3a)): This element is optional.

-   **Part** (section [2.2.2.33](#Section_2e08aa0ad12e4b9ab3979e32ef4186b5)): This element is optional.

-   **Preview** (section [2.2.2.35.1](#Section_47ef04acfcfb4f0e81ec81d6ff10108f)): This element is optional.

When the **Body** element is a child of the **meetingresponse:SendResponse** element or the **composemail:SmartForward** element, it has only the child elements **Type** and **Data**.

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

For the **Calendar**, **Contact**, **Email**, and **Tasks** content classes, the **Body** element that is defined in the respective class namespace is used with protocol version 2.5 instead of the **Body** element of the **AirSyncBase** namespace. For details, see the [\[MS-ASCAL\]](%5bMS-ASCAL%5d.pdf#Section_0c4486824a6a459aae662fed0712bef9), [\[MS-ASCNTC\]](%5bMS-ASCNTC%5d.pdf#Section_a4593b9dd9af4d27bc5c67c4c1b98d54), [\[MS-ASEMAIL\]](%5bMS-ASEMAIL%5d.pdf#Section_f3d27369e0f54164aa5e9b1abda16f5f), and [\[MS-ASTASK\]](%5bMS-ASTASK%5d.pdf#Section_b8fe266450ba4d00bf6be4deab352c89) documents.

The **Body** element is a child of the **meetingresponse:SendResponse** element and the **composemail:SmartForward** element only when protocol version 16.0 or 16.1 is used.

#### BodyPart

The **BodyPart** element is an optional child element of the **airsync:ApplicationData** element that specifies details about the message part of an e-mail in a response. The **BodyPart** element MUST be included in a command response when the **BodyPartPreference** element (section [2.2.2.11](#Section_6d9d09fd2ebc4570ba5cd6af546b3b12)) is specified in a request.

The **BodyPart** element is a **container** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2.

There is no limit on the number of **BodyPart** elements in a command response. Command requests MUST NOT include the **BodyPart** element. In a response, the **airsync:ApplicationData** element MUST be the parent element of the **BodyPart** element.

The **BodyPart** element, if present, MUST have its required and optional child elements in the following order:

-   **Status** (section [2.2.2.37](#Section_e1ec362012314d11b57752bb6a1d77be)). This element is required.

-   **Type** (section [2.2.2.41.2](#Section_84a14e369b164757aa6566dbaa4c1d62)). This element is required.

-   **EstimatedDataSize** (section [2.2.2.23.3](#Section_32c5e925fd624b7d8ffaa7d1199fe603)). This element is required.

-   **Truncated** (section [2.2.2.39.2](#Section_eb254c9d777a4156adb67f6d1576f5c6)). This element is optional.

-   **Data** (section [2.2.2.20.2](#Section_8591cbbf434c4729bc3abf4f6d90902b)). This element is optional.

-   **Preview** (section [2.2.2.35.2](#Section_f4ea50c9fae54229b0433c4e2295d096)). This element is optional.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### BodyPartPreference

The **BodyPartPreference** element is an optional element that sets preference information related to the type and size of information that is returned from searching, synchronizing, or fetching a message part.

The **BodyPartPreference** element is a **container** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2.

A command response MUST NOT include a **BodyPartPreference** element. Command requests can include the **BodyPartPreference** element. The **Options** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.125) MUST be the parent element of the **BodyPartPreference** element. The **BodyPartPreference** element, if present, MUST have the following required and optional child elements in the following order:

-   **Type** (section [2.2.2.41.3](#Section_7044e1712c5945e98de77ba8b74e1f3f)). This element is required.

-   **TruncationSize** (section [2.2.2.40.1](#Section_b672685019324420b0a22a892d97043c)). This element is optional.

-   **AllOrNone** (section [2.2.2.3.1](#Section_a0679789f0c149ca95b2348b8852027a)). This element is optional.

-   **Preview** (section [2.2.2.35.3](#Section_f00efde43d9346b1945a20346b53697f)). This element is optional.

The contents of the **Options** element specify preferences for all of the content that the user is interested in searching, synchronizing, or retrieving. These preferences are set on a per-request basis and override any stored information. Because this information is required to process every request, the information can be persisted on the server if network load is a concern.

There MUST be one explicit **BodyPartPreference** element for each **Type** value specified in the set of preferences in order to request a **BodyPart** element (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5)) of that **Type** in the response.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### BodyPreference

The **BodyPreference** element is an optional element that sets preference information related to the type and size of information that is returned from searching, synchronizing, or fetching.

The **BodyPreference** element is a **container** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2.

A command response MUST NOT include the **BodyPreference** element. Command requests can include the **BodyPreference** element. The **BodyPreference** element, if present, has the following child elements in this order:

-   **Type** (section [2.2.2.41.4](#Section_5a816c90c5fe4b4299ac5f85c7f3efb6)). This element is required.

-   **TruncationSize** (section [2.2.2.40.2](#Section_acaa68c410fc403390cc45a5849b196d)). This element is optional.

-   **AllOrNone** (section [2.2.2.3.2](#Section_ca562432dee14a4da5fe9e6c20a3ced7)). This element is optional.

-   **Preview** (section [2.2.2.35.4](#Section_8932fcdb468c40e69aee380f78736019)). This element is optional.

The contents of the **airsync:Options**, **itemoperations:Options**, or **search:Options** element specify preferences for all of the content that the user is interested in searching, synchronizing, or retrieving. These preferences are persisted by the server from request to request for the specified client, and can be changed by the inclusion of an **airsync:Options** element in any subsequent request.

A request MUST NOT contain more than one **BodyPreference** element for each allowable value of the **Type** element.

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

#### City

The **City** element is an optional child element of the **Location** element (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0)) that specifies the city in which an event occurs.

The **City** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### ClientId

The **ClientId** element is a required child element of the **Add** element (section [2.2.2.2](#Section_d60d864c07304b20bd74cc0245d4cdc6)) in a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) and a required child element of the **Attachment** element (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda)) in a **Sync** command response. The **ClientId** element specifies a client-generated temporary identifier that links to the file that is being added as an attachment.

The **ClientId** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

The server will return the **ClientId** element along with the **FileReference** element (section [2.2.2.24.1](#Section_9f86ef66b7ed48ad909f15c0e338fac8)) as child elements of the **Attachment** element in response to a **Sync** command request that adds an attachment either to a [**calendar**](#gt_7204b2ed-dcef-4434-be15-6451f92d03fb) item or to a draft email item.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Content

The **Content** element is a required child element of the **Add** element (section [2.2.2.2](#Section_d60d864c07304b20bd74cc0245d4cdc6)) that contains the content of the attachment that is being added.

The **Content** element is a **string** data type **byte array**, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.1.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### ContentId

The **ContentId** element is a child element of the **Add** element (section [2.2.2.2](#Section_d60d864c07304b20bd74cc0245d4cdc6)) and the **Attachment** element (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda)). For more details about the **ContentId** element, see sections [2.2.2.16.1](#Section_8a4746a7756a414687d1270d1c3e2f4f) and [2.2.2.16.2](#Section_be51de6eeb8341fbb79ca02d762c2ed0).

##### ContentId (Add)

The **ContentId** element is an optional child element of the **Add** element (section [2.2.2.2](#Section_d60d864c07304b20bd74cc0245d4cdc6)) that specifies the unique object identifier of an attachment that is being added to a [**calendar**](#gt_7204b2ed-dcef-4434-be15-6451f92d03fb) item or to a draft email item. This identifier is used to reference the attachment within the item to which the attachment belongs.

The **ContentId** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

The **Add** element MUST have a maximum of one **ContentId** element.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

##### ContentId (Attachment)

The **ContentId** element is an optional child element of the **Attachment** element (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda)) that contains the unique identifier of the attachment, and is used to reference the attachment within the item to which the attachment belongs.

The **ContentId** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

A command response MUST have a maximum of one **ContentId** element per **Attachment** element.

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

#### ContentLocation

The **ContentLocation** element is a child element of the **Add** element (section [2.2.2.2](#Section_d60d864c07304b20bd74cc0245d4cdc6)) and the **Attachment** element (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda)). For more details about the **ContentLocation** element, see sections [2.2.2.17.1](#Section_0bf6e69606bc423f96f4e80a6ad1f438) and [2.2.2.17.2](#Section_fe8bead0e57c4948ad5d68867e4c3f76).

##### ContentLocation (Add)

The **ContentLocation** element is an optional child element of the **Add** element (section [2.2.2.2](#Section_d60d864c07304b20bd74cc0245d4cdc6)) that specifies the relative [**URI**](#gt_e18af8e8-01d7-4f91-8a1e-0fb21b191f95) for an attachment that is being added to a [**calendar**](#gt_7204b2ed-dcef-4434-be15-6451f92d03fb) item or to a draft email item. This URI is used to associate the attachment in other items.

The **ContentLocation** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

The **Add** element MUST have a maximum of one **ContentLocation** element.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

##### ContentLocation (Attachment)

The **ContentLocation** element is an optional child element of the **Attachment** element (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda)) that contains the relative [**URI**](#gt_e18af8e8-01d7-4f91-8a1e-0fb21b191f95) for an attachment, and is used to associate the attachment in other items with URI defining its location.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

A command response MUST have a maximum of one **ContentLocation** element per **Attachment** element. The **ContentLocation** element MUST have no child elements.

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

#### ContentType

The **ContentType** element is a child element of the **Add** element (section [2.2.2.2](#Section_d60d864c07304b20bd74cc0245d4cdc6)) and the **itemoperations:Properties** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.139.2). For more details about the **ContentType** element, see sections [2.2.2.18.1](#Section_5c9b829b1de9400086382525d17dd850) and [2.2.2.18.2](#Section_735e38c609d04e2ca5fbe1d2394bd808).

##### ContentType (Add)

The **ContentType** element is an optional child element of the **Add** element (section [2.2.2.2](#Section_d60d864c07304b20bd74cc0245d4cdc6)) that specifies the type of data contained in the **Content** element (section [2.2.2.15](#Section_81bede41154743ef89f0d85e0e0ed2c1)) for an attachment that is being added to a [**calendar**](#gt_7204b2ed-dcef-4434-be15-6451f92d03fb) item or to a draft email item.

The **ContentType** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

The **Add** element MUST have a maximum of one **ContentType** element.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

##### ContentType (Properties)

The **ContentType** element is an optional child element of the **itemoperations:Properties** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.139.2) that specifies the type of data returned in the **itemoperations:Data** element (\[MS-ASCMD\] section 2.2.3.39.2) of an **ItemOperations** command response (\[MS-ASCMD\] section 2.2.1.10).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

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

#### Country

The **Country** element is an optional child element of the **Location** element (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0)) that specifies the country in which an event occurs.

The **Country** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Data

The **Data** element is a child element of the **Body** element (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de)) and the **BodyPart** element (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5)) that contains the data of the [**message body**](#gt_f8f4c2f5-c760-4abe-a9a1-573302980088) or the message part of the calendar item, contact, document, e-mail, or task.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

##### Data (Body)

The **Data** element is an optional child element of the **Body** element (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de)). A command response MUST have a maximum of one **Data** element within each returned **Body** element. Command requests can include the **Data** element. This element MUST NOT be present in multipart responses, as specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10.1.

The content of the **Data** element is returned as a **string** in the format that is specified by the **Type** element (section [2.2.2.41.1](#Section_02747a2d938143f499a9e854c6422901)). If the value of the **Type** element is 3 ([**RTF**](#gt_a9aa8673-7798-4eba-a048-8b7c95a7b080)), the value of the **Data** element is encoded using [**base64 encoding**](#gt_179b9392-9019-45a3-880b-26f6890522b7).

If the **Truncated** element (section [2.2.2.39.1](#Section_cada9eae58664657b11ea0feac6804e7)) is included in the response, the data in the **Data** element is truncated. The **EstimatedDataSize** element (section [2.2.2.23.2](#Section_108e3cbcc8b94b77a088e88c7bb4a8aa)) provides a rough estimation of the actual size of the complete content of the **Data** element.

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

##### Data (BodyPart)

The **Data** element is an optional child element of the **BodyPart** element (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5)). A command response MUST have a maximum of one **Data** element within each returned **BodyPart** element.

In a response, the **Data** element MUST have no child elements.

The content of the **Data** element is returned as a **string** in the format that is specified by the **Type** element (section [2.2.2.41.2](#Section_84a14e369b164757aa6566dbaa4c1d62)). If the value of the **Type** element is 3 ([**RTF**](#gt_a9aa8673-7798-4eba-a048-8b7c95a7b080)), the value of the **Data** element is encoded using [**base64 encoding**](#gt_179b9392-9019-45a3-880b-26f6890522b7).

If the **Truncated** element (section [2.2.2.39.2](#Section_eb254c9d777a4156adb67f6d1576f5c6)) is included in the response, then the data in the **Data** element is truncated. The **EstimatedDataSize** element (section [2.2.2.23.3](#Section_32c5e925fd624b7d8ffaa7d1199fe603)) provides a rough estimation of the actual size of the complete content of the **Data** **string**.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Delete

The **Delete** element is an optional child element of the **Attachments** element (section [2.2.2.8](#Section_12f116075694421ab7d1e436da4da613)) that deletes an attachment from a [**calendar**](#gt_7204b2ed-dcef-4434-be15-6451f92d03fb) item or from a draft email item.

The **Delete** element is a **container** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2. It has the following child elements:

-   **FileReference** (section [2.2.2.24.2](#Section_5a6ef0d3d72640b997b79e6dbba2c59d)) --- This element is required.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### DisplayName

The **DisplayName** element is a child element of the **Add** element (section [2.2.2.2](#Section_d60d864c07304b20bd74cc0245d4cdc6)), the **Attachment** element (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda)), and the **Location** element (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0)). For more details about the **DisplayName** element, see sections [2.2.2.22.1](#Section_2ddce3aaee104aacb717cd3ec6506704) through [2.2.2.22.3](#Section_7497e46cce6c44b9aa7dc64951188dad).

##### DisplayName (Add)

The **DisplayName** element is a required child element of the **Add** element (section [2.2.2.2](#Section_d60d864c07304b20bd74cc0245d4cdc6)) that specifies the display name of an attachment that is being added to a [**calendar**](#gt_7204b2ed-dcef-4434-be15-6451f92d03fb) item or to a draft email item.

The **DisplayName** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

The **Add** element MUST have a maximum of one **DisplayName** element.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

##### DisplayName (Attachment)

The **DisplayName** element is an optional child element of the **Attachment** element (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda)) that specifies the display name of the attachment.

The value of this element is a **string** value ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7).

A command response MUST have a maximum of one **DisplayName** element per **Attachment** element.

The **DisplayName** element MUST have no child elements.

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

##### DisplayName (Location)

The **DisplayName** element is an optional child element of the **Location** element (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0)) that specifies the display name of an event\'s location.

The **DisplayName** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

The **Location** element MUST have a maximum of one **DisplayName** element.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### EstimatedDataSize

The **EstimatedDataSize** element is a child element of the **Attachment** element (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda)), the **Body** element (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de)), and the **BodyPart** element (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5)) that provides an informational estimate of the size of the data associated with the parent element.

The value of this element is an **integer** value ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6).

The **EstimatedDataSize** value represents an estimate of the original size of the content in the [**message store**](#gt_fda94a53-448d-48d5-9991-176c530ff597) and is specified in bytes. This number is only an estimate, and the actual size of the content when fetched can differ based on the content filtering rules applied.

##### EstimatedDataSize (Attachment)

The **EstimatedDataSize** element is required child element of the **Attachment** element (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda)).

A command response MUST have a maximum of one **EstimatedDataSize** element per **Attachment** element.

The **EstimatedDataSize** element MUST have no child elements.

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

##### EstimatedDataSize (Body)

The **EstimatedDataSize** element is an optional child element of the **Body** element (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de)). The **EstimatedDataSize** element SHOULD be included in a response message whenever the **Truncated** element is set to TRUE.

A command response MUST have a maximum of one **EstimatedDataSize** element per **Body** element.

The **EstimatedDataSize** element MUST have no child elements.

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

##### EstimatedDataSize (BodyPart)

The **EstimatedDataSize** element is a required child element of the **BodyPart** element (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5)). The **EstimatedDataSize** element SHOULD be included in a response message whenever the **Truncated** element is set to TRUE.

A command response MUST have a maximum of one **EstimatedDataSize** element per **BodyPart** element. The **EstimatedDataSize** element MUST have no child elements.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### FileReference

The **FileReference** element is a child element of the **itemoperations:Fetch** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.67.1), the **Delete** element (section [2.2.2.21](#Section_b9f6e6cdfe714014a148cb21694f9a80)), and the **Attachment** element (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda)). For more details about the **FileReference** element, see sections [2.2.2.24.1](#Section_9f86ef66b7ed48ad909f15c0e338fac8) through [2.2.2.24.3](#Section_bf288cfbc2544398af56cdc2f03e127c).

##### FileReference (Attachment)

The **FileReference** element is a required child element of the **Attachment** element (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda)) that specifies the location of an item on the server to retrieve.

The **FileReference** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

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

In protocol version 16.0 and 16.1, the server will return the **FileReference** element along with the **ClientId** element (section [2.2.2.14](#Section_760294dc8295419eace3700bfa7c7266)) as child elements of the **Attachment** element in response to a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) that adds an attachment either to a [**calendar**](#gt_7204b2ed-dcef-4434-be15-6451f92d03fb) item or to a draft email item. The client MUST record the value of the **FileReference** element that is returned. This value will be used to specify the attachment in a future **Sync** command request if the client deletes the attachment.

##### FileReference (Delete)

The **FileReference** element is a required child element of the **Delete** element (section [2.2.2.21](#Section_b9f6e6cdfe714014a148cb21694f9a80)) that specifies the server-assigned unique identifier of the attachment to be deleted.

The **FileReference** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

A unique identifier is assigned to the attachment when the attachment is added. This identifier is returned to the client in the **FileReference** element of the **Sync** command response, as specified in section [2.2.2.24.1](#Section_9f86ef66b7ed48ad909f15c0e338fac8). To specify a particular attachment for deletion, the client uses the same identifier that was returned in the **FileReference** element when the attachment was added.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

The **FileReference** element is not supported as a child of the **Delete** element in protocol versions 2.5, 12.0, 12.1, 14.0, and 14.1.

##### FileReference (Fetch)

In an **ItemOperations** command request (as specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10), the **FileReference** element is an optional child element of the **itemoperations:Fetch** element (as specified in \[MS-ASCMD\] section 2.2.3.67.1). The **FileReference** element specifies a unique identifier that is assigned by the server to each [**Attachment object**](#gt_6ab4cacc-0e1a-4843-b9e5-4f1fee5a695a) to a [**Message object**](#gt_b6c15d0c-d992-421d-ba96-99d3b63894cf).

The **FileReference** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

If the client includes a zero-length string for the value of this element in an **ItemOperations** command request, the server responds with a protocol status error of 15.

The **FileReference** element MUST have no child elements.

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

#### InstanceId

The **InstanceId** element specifies the original, unmodified, UTC date and time of a particular instance of a [**recurring series**](#gt_2325d666-e02f-49e4-afa5-3e896d672efe). The **InstanceId** element is a child element of the **calendar:Exception** element ([\[MS-ASCAL\]](%5bMS-ASCAL%5d.pdf#Section_0c4486824a6a459aae662fed0712bef9) section 2.2.2.21) in a **Sync** command request and response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) and a child element of the **airsync:ApplicationData** element (\[MS-ASCMD\] section 2.2.3.11) in a **Sync** command response for an [**orphan instance**](#gt_0efee4a8-a2e9-48fe-87f8-d45097de6b72). The **InstanceId** element is a child element of the **airsync:Change** element (\[MS-ASCMD\] section 2.2.3.24) or the **airsync:Delete** element (\[MS-ASCMD\] section 2.2.3.42.2) in a **Sync** command request. The server will include the **InstanceId** element along with the **ServerId** element (\[MS-ASCMD\] section 2.2.3.166.8) in any **Sync** command response to the client\'s **Sync** command request.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7. The string MUST be formatted as a **Compact DateTime**, as specified in \[MS-ASDTYPE\] section 2.7.2.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### IsInline

The **IsInline** element is a child element of the **Add** element (section [2.2.2.2](#Section_d60d864c07304b20bd74cc0245d4cdc6)) and the **Attachment** element (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda)). For more details about the **IsInline** element, see sections [2.2.2.26.1](#Section_b427fb12e516406399c0c3f1a3a2cdcd) and [2.2.2.26.2](#Section_c7e6d9c8b69a4e0c95769e55de793b65).

##### IsInline (Add)

The **IsInline** element is an optional child element of the **Add** element (section [2.2.2.2](#Section_d60d864c07304b20bd74cc0245d4cdc6)) that indicates whether the attachment being added is embedded in the message.

The **IsInline** element is an empty tag element, meaning it has no value or data type. It is distinguished only by the presence or absence of the \<IsInline/\> tag. Presence of the tag indicates that the attachment is embedded in the message; absence indicates that the attachment is not embedded.

The **Add** element MUST have a maximum of one **IsInline** element.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

##### IsInline (Attachment)

The **IsInline** element is an optional child element of the **Attachment** element (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda)) that specifies whether the attachment is embedded in the message.

The value of this element is a **boolean** value ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.1).

A command response MUST have a maximum of one **IsInline** element per **Attachment** element.

The **IsInline** element MUST have no child elements.

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

#### Latitude

The **Latitude** element is an optional child element of the **Location** element (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0)) that specifies the latitude of the event\'s location.

The **Latitude** element is a **double** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.4.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Location

The **Location** element specifies details about the location of an event. This element is an optional child element of the following elements:

-   **airsync:ApplicationData** ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.11), in a command request or a command response

-   **itemoperations:Schema** (\[MS-ASCMD\] section 2.2.3.158), in a command request

```{=html}
<!-- -->
```
-   **itemoperations:Properties** (\[MS-ASCMD\] section 2.2.3.139.2), in a command response

-   **search:Properties** (\[MS-ASCMD\] section 2.2.3.139.3), in a command response

```{=html}
<!-- -->
```
-   **calendar:Exception** ([\[MS-ASCAL\]](%5bMS-ASCAL%5d.pdf#Section_0c4486824a6a459aae662fed0712bef9) section 2.2.2.21), in a command request or a command response

-   **email:MeetingRequest** ([\[MS-ASEMAIL\]](%5bMS-ASEMAIL%5d.pdf#Section_f3d27369e0f54164aa5e9b1abda16f5f) section 2.2.2.48), in a command response

The **Location** element is **container** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2. The client\'s request can include an empty **Location** element to remove the location from an item. The **Location** element has the following child elements, all of which are optional:

-   **Accuracy** (section [2.2.2.1](#Section_13318ff160fe435c81d86a0a6ac92863))

-   **Altitude** (section [2.2.2.4](#Section_cd8db29335c8457b867cdb2ba10cda59))

-   **AltitudeAccuracy** (section [2.2.2.5](#Section_6b87364e7cda485691298775159a61aa))

-   **Annotation** (section [2.2.2.6](#Section_68b6b09643a745cf9f61492334efaced))

-   **City** (section [2.2.2.13](#Section_00241f454bbf461880f04e74584121e0))

-   **Country** (section [2.2.2.19](#Section_41cd8a0438b1493bb0bdb600429bc531))

-   **DisplayName** (section [2.2.2.22.3](#Section_7497e46cce6c44b9aa7dc64951188dad))

-   **Latitude** (section [2.2.2.27](#Section_3c07ac1c46a44b89a5eff5abc30b297c))

-   **LocationUri** (section [2.2.2.29](#Section_8256aa9c53b4427f9f95d3ae17f38c02))

-   **Longitude** (section [2.2.2.30](#Section_7f62f1e593be4675b389b63903c5d472))

-   **PostalCode** (section [2.2.2.34](#Section_bfa47960d8844de299a42023647f5b41))

-   **State** (section [2.2.2.36](#Section_efbda0bb756f4ecdace411bc5477dec0))

-   **Street** (section [2.2.2.38](#Section_bbf2888e23344087afa54551e2d400ce))

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

The **calendar:Location** element, as specified in \[MS-ASCAL\] section 2.2.2.27, and the **email:Location** element, as specified in \[MS-ASEMAIL\] section 2.2.2.46, are used with protocol versions 2.5, 12.0, 12.1, 14.0, and 14.1 instead of the **Location** element of the **AirSyncBase** namespace.

#### LocationUri

The **LocationUri** element is an optional child element of the **Location** element (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0)) that specifies the [**URI**](#gt_e18af8e8-01d7-4f91-8a1e-0fb21b191f95) for the location of an event.

The **LocationUri** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Longitude

The **Longitude** element is an optional child element of the **Location** element (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0)) that specifies the longitude of the event\'s location.

The **Longitude** element is a **double** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.4.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Method

The **Method** element is a child element of the **Add** element (section [2.2.2.2](#Section_d60d864c07304b20bd74cc0245d4cdc6)) and the **Attachment** element (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda)). For more details about the **Method** element, see sections [2.2.2.31.1](#Section_ce0e5014f5b247f8ba377983dbb2a8d0) and [2.2.2.31.2](#Section_a1408a2d517848958103e66b435432be).

##### Method (Add)

The **Method** element is a required child element of the **Add** element (section [2.2.2.2](#Section_d60d864c07304b20bd74cc0245d4cdc6)) that identifies the method in which the attachment to be added was attached.

The **Method** element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The **Add** element MUST have a maximum of one **Method** element. The following table lists the possible values of the **Method** element as a child element of the **Add** element.

  -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Value   Meaning             Notes
  ------- ------------------- -------------------------------------------------------------------------------------------------------------------------------------------------------
  1       Normal attachment   The attachment is a normal attachment. This value is valid for a [**calendar**](#gt_7204b2ed-dcef-4434-be15-6451f92d03fb) item or a draft email item.

  5       Embedded message    The attachment is an email message and the attachment file has an .eml extension. This value is valid only for a draft email item.
  -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

##### Method (Attachment)

The **Method** element is a required child element of the **Attachment** element (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda)) that identifies the method in which the attachment was attached.

The **Method** element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

A command response MUST have a maximum of one **Method** element per **Attachment** element.

The **Method** element MUST have no child elements.

The following table defines the valid values of the **Method** element.

  ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Value   Meaning             Notes
  ------- ------------------- ------------------------------------------------------------------------------------------------------------------------------------------------------------------
  1       Normal attachment   The attachment is a normal attachment. This is the most common value.

  2       Reserved            Do not use.

  3       Reserved            Do not use.

  4       Reserved            Do not use.

  5       Embedded message    Indicates that the attachment is an e-mail message, and that the attachment file has an .eml extension.

  6       Attach OLE          Indicates that the attachment is an embedded [**Object Linking and Embedding (OLE)**](#gt_171744b8-3f44-4198-b7b9-1c0147282d2c) object, such as an inline image.
  ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

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

#### NativeBodyType

The **NativeBodyType** element is an optional child element of the **airsync:ApplicationData** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a)) in the **Sync** command that specifies the original format type of the item.

The value of this element is an **unsignedByte** value ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8).

A command response MUST have a maximum of one **NativeBodyType** element per **airsync:ApplicationData** element. Command requests can include the **NativeBodyType** element.

The **NativeBodyType** element MUST have no child elements.

The following table defines the valid values of the **NativeBodyType** element.

  -----------------------------------------------------------------------
  Value                      Description
  -------------------------- --------------------------------------------
  1                          Plain text

  2                          HTML

  3                          RTF
  -----------------------------------------------------------------------

The **NativeBodyType** and **Type** elements have the same value unless the server has modified the format of the body to match the client\'s request. The client can specify a preferred body format by using the **Type** element of a **Search** or **Sync** command request.

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

#### Part

The **itemoperations:Part** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.130) is an optional child element of the **Body** element (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de)) that contains an integer index into the metadata of the multipart response. This element MUST be present in multipart responses, as specified in \[MS-ASCMD\] section 2.2.1.10.1. This element MUST NOT be present in requests or non-multipart responses.

The value of this element is an **integer** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6).

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
  -----------------------------------------------------------------------

#### PostalCode

The **PostalCode** element is an optional child element of the **Location** element (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0)) that specifies the postal code for the address of the event\'s location.

The **PostalCode** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Preview

The **Preview** element is a child element of the **Body** element (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de)), the **BodyPart** element (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5)), the **BodyPartPreference** element (section [2.2.2.11](#Section_6d9d09fd2ebc4570ba5cd6af546b3b12)), and the **BodyPreference** element (section [2.2.2.12](#Section_793666561b7e4564a930ffb6fa85d283)).

##### Preview (Body)

The **Preview** element is an optional child element of the **Body** element (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de)) that contains the [**Unicode**](#gt_c305d0ab-8b94-461a-bd76-13b40cb8c4d8) plain text message or message part preview returned to the client.

The value of this element is a **string** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7). The **Preview** element in a response MUST contain no more than the number of characters specified in the request.

Command responses MUST have a maximum of one **Preview** element per **Body** element.

If the **Body** element in the request contains a **Type** element of value 1 (Plain text) and there is valid data returned in the **Data** element (section [2.2.2.20.1](#Section_b673d5ba6afe466d97722a0ceaa49c3a)), then the **Preview** element will not be returned in the same response.

The **Preview** element MUST have no child elements.

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

##### Preview (BodyPart)

The **Preview** element is an optional child element of the **BodyPart** element (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5)) that contains the [**Unicode**](#gt_c305d0ab-8b94-461a-bd76-13b40cb8c4d8) plain text message or message part preview returned to the client.

The value of this element is a **string** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7). The **Preview** element in a response MUST contain no more than the number of characters specified in the request. The **Preview** element MUST be present in a command response if a **BodyPartPreference** element (section [2.2.2.11](#Section_6d9d09fd2ebc4570ba5cd6af546b3b12)) in the request included a **Preview** element and the server can honor the request.

Command responses MUST have a maximum of one **Preview** element per **BodyPart** element.

The **Preview** element MUST have no child elements.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

##### Preview (BodyPartPreference)

The **Preview** element is an optional child element of the **BodyPartPreference** element (section [2.2.2.11](#Section_6d9d09fd2ebc4570ba5cd6af546b3b12)) that specifies the maximum length of the [**Unicode**](#gt_c305d0ab-8b94-461a-bd76-13b40cb8c4d8) plain text message or message part preview to be returned to the client.

The value of this element is an **integer** value ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6).This element MUST have a value set from 0 to 255, inclusive.

A command request MUST have a maximum of one **Preview** element per **BodyPartPreference** element.

The **Preview** element MUST have no child elements.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

##### Preview (BodyPreference)

The **Preview** element is an optional child element of the **BodyPreference** element (section [2.2.2.12](#Section_793666561b7e4564a930ffb6fa85d283)) that specifies the maximum length of the [**Unicode**](#gt_c305d0ab-8b94-461a-bd76-13b40cb8c4d8) plain text message or message part preview to be returned to the client.

The value of this element is an **integer** value (as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6). This element MUST have a value set from 0 to 255, inclusive.

A command request MUST have a maximum of one **Preview** element per **BodyPreference** element.

The **Preview** element MUST have no child elements.

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

#### State

The **State** element is an optional child element of the **Location** element (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0)) that specifies the state or province in which an event occurs.

The **State** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Status

The **Status** element is a required child element of the **BodyPart** element (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5)) that indicates the success or failure of the response in returning **Data** element content (section [2.2.2.20.2](#Section_8591cbbf434c4729bc3abf4f6d90902b)) given the **BodyPartPreference** element settings (section [2.2.2.11](#Section_6d9d09fd2ebc4570ba5cd6af546b3b12)) in the request.

The **Status** element is an **enumeration** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.5.

The following table lists valid values for the **Status** element.

  -----------------------------------------------------------------------
  Value           Meaning
  --------------- -------------------------------------------------------
  1               Success.

  176             The message part is too large.
  -----------------------------------------------------------------------

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Street

The **Street** element is an optional child element of the **Location** element (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0)) that specifies the street address of the event\'s location.

The **Street** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### Truncated

The **Truncated** element is a child element of the **Body** element (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de)) and the **BodyPart** element (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5)). The value of this element is a **boolean** value ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.1) that specified whether the body or body part has been truncated.

##### Truncated (Body)

The **Truncated** element is an optional child element of the **Body** element (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de)) that specifies whether the body of the item has been truncated according to the **BodyPreference** element (section [2.2.2.12](#Section_793666561b7e4564a930ffb6fa85d283)) indicated by the client.

If the value is TRUE, then the body of the item has been truncated. If the value is FALSE, or there is no **Truncated** element, then the body of the item has not been truncated.

If a **Truncated** element is included in a command request, then it is ignored by the server.

A command response MUST have a maximum of one **Truncated** element per **Body** element.

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

When protocol version 16.0 or 16.1 is used, the **Truncated** element MUST NOT be included in a command request.

##### Truncated (BodyPart)

The **Truncated** element is an optional child element of the **BodyPart** element (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5)) that specifies whether the body of the item has been truncated according to the **BodyPartPreference** element (section [2.2.2.11](#Section_6d9d09fd2ebc4570ba5cd6af546b3b12)) indicated by the client.

If the value is TRUE, then the body of the item has been truncated. If the value is FALSE, or there is no **Truncated** element, then the body of the item has not been truncated.

A command response MUST have a maximum of one **Truncated** element per **BodyPart** element.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

#### TruncationSize

The **TruncationSize** element is a child element of the **BodyPartPreference** element (section [2.2.2.11](#Section_6d9d09fd2ebc4570ba5cd6af546b3b12)) and the **BodyPreference** element (section [2.2.2.12](#Section_793666561b7e4564a930ffb6fa85d283)). The value of this element is an **integer** value ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6) that specifies the size, in bytes, of the content that the user wants to search, synchronize, or fetch.

##### TruncationSize (BodyPartPreference)

The **TruncationSize** element is an optional child element of the **BodyPartPreference** element (section [2.2.2.11](#Section_6d9d09fd2ebc4570ba5cd6af546b3b12)).

A command request MUST have a maximum of one **TruncationSize** element per **BodyPartPreference** element.

Command responses MUST NOT include the **TruncationSize** element.

The **TruncationSize** element MUST have no child elements.

The maximum value for **TruncationSize** is 4,294,967,295. If the **TruncationSize** element is absent, the entire content is used for the request.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

##### TruncationSize (BodyPreference)

The **TruncationSize** element is an optional child element of the **BodyPreference** type (section [2.2.2.12](#Section_793666561b7e4564a930ffb6fa85d283)).

A command request MUST have a maximum of one **TruncationSize** element per **BodyPreference** element.

Command responses MUST NOT include the **TruncationSize** element.

The **TruncationSize** element MUST have no child elements.

The maximum value for **TruncationSize** is 4,294,967,295. If the **TruncationSize** element is absent, the entire content is used for the request.

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

#### Type

The **Type** element is a child element of the **Body** element (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de)), the **BodyPart** element (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5)), the **BodyPartPreference** element (section [2.2.2.11](#Section_6d9d09fd2ebc4570ba5cd6af546b3b12)), and the **BodyPreference** element (section [2.2.2.12](#Section_793666561b7e4564a930ffb6fa85d283)). The value of this element is an **unsignedByte** value ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8) that indicates the format type of the body content of the item.

The following table defines the valid values of the **Type** element.

  ---------------------------------------------------------------------------------
  Value                      Description
  -------------------------- ------------------------------------------------------
  1                          Plain text

  2                          [**HTML**](#gt_549c4960-e8be-4c24-bc2b-b86530f1c1bf)

  3                          [**RTF**](#gt_a9aa8673-7798-4eba-a048-8b7c95a7b080)

  4                          [**MIME**](#gt_af6ba277-34c1-493d-8103-71d2af36ce30)
  ---------------------------------------------------------------------------------

##### Type (Body)

The **Type** element is a required child element of the **Body** element (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de)).

A command request or response MUST have a maximum of one **Type** element per **Body** element.

The **Type** element MUST have no child elements.

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

For [**calendar**](#gt_7204b2ed-dcef-4434-be15-6451f92d03fb) items in protocol version 16.0 or 16.1, the only valid values for this element are 1 (plain text) and 2 (HTML).

##### Type (BodyPart)

The **Type** element is a required child element of the **BodyPart** element (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5)).

A command response MUST have a maximum of one **Type** element per **BodyPart** element.

The **Type** element MUST have no child elements.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

##### Type (BodyPartPreference)

The **Type** element is a required child element of the **BodyPartPreference** element (section [2.2.2.11](#Section_6d9d09fd2ebc4570ba5cd6af546b3b12)).

A command request MUST have a maximum of one **Type** element per **BodyPartPreference** element.

The **Type** element MUST have no child elements.

Only a value of 2 (HTML) SHOULD be used in the **Type** element of a **BodyPartPreference** element.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Element support
  ----------------------------------- -----------------------------------
  2.5                                 

  12.0                                

  12.1                                

  14.0                                

  14.1                                Yes

  16.0                                Yes

  16.1                                Yes
  -----------------------------------------------------------------------

##### Type (BodyPreference)

The **Type** element is a required child element of the **BodyPreference** element (section [2.2.2.12](#Section_793666561b7e4564a930ffb6fa85d283)).

A command request MUST have a maximum of one **Type** element per **BodyPreference** element.

The **Type** element MUST have no child elements.

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

### Groups

The following table summarizes the set of common [**XML schema**](#gt_bd0ce6f9-c350-4900-827e-951265294067) group definitions defined by this specification. XML schema groups that are specific to a particular operation are described with the operation.

  --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Group                     Description
  ------------------------- ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  **TopLevelSchemaProps**   Identifies the **Body** element (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de)), **BodyPart** element (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5)), and the **Attachments** element (section [2.2.2.8](#Section_12f116075694421ab7d1e436da4da613)) as being part of the **TopLevelSchemaProps** group.

  --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

#### TopLevelSchemaProps

The **TopLevelSchemaProps** group identifies the **Body** element (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de)), the **BodyPart** element (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5)), and the **Attachments** element (section [2.2.2.8](#Section_12f116075694421ab7d1e436da4da613)) as being part of the **TopLevelSchemaProps** group. The **TopLevelSchemaProps** group is used by the **ItemOperations** command request specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10.

**Protocol Versions**

The following table specifies the protocol versions that support this group. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

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

# Protocol Details

## Client Details

### Abstract Data Model

None.

### Timers

None.

### Initialization

None.

### Higher-Layer Triggered Events

None.

### Message Processing Events and Sequencing Rules

#### Commands

The following table lists the commands that use the XML elements specified by this protocol.

  --------------------------------------------------------------------------------------------------------
  Command               Description
  --------------------- ----------------------------------------------------------------------------------
  **ItemOperations**    Retrieves an item from the server.

  **MeetingResponse**   Specifies a user\'s response to a meeting request.

  **Search**            Searches the server for items that match the specified criteria.

  **SmartForward**      Forwards messages without retrieving the full, original message from the server.

  **Sync**              Synchronizes changes in a collections set between the client and the server.
  --------------------------------------------------------------------------------------------------------

##### ItemOperations

The request message for the **ItemOperations** command can include the following elements:

-   **FileReference** (section [2.2.2.24.3](#Section_bf288cfbc2544398af56cdc2f03e127c))

-   **BodyPreference** (section [2.2.2.12](#Section_793666561b7e4564a930ffb6fa85d283))

    -   **Type** (section [2.2.2.41.4](#Section_5a816c90c5fe4b4299ac5f85c7f3efb6))

    -   **TruncationSize** (section [2.2.2.40.2](#Section_acaa68c410fc403390cc45a5849b196d))

    -   **AllOrNone** (section [2.2.2.3.2](#Section_ca562432dee14a4da5fe9e6c20a3ced7))

    -   **Preview** (section [2.2.2.35.4](#Section_8932fcdb468c40e69aee380f78736019))

-   **BodyPartPreference** (section [2.2.2.11](#Section_6d9d09fd2ebc4570ba5cd6af546b3b12))

    -   **Type** (section [2.2.2.41.3](#Section_7044e1712c5945e98de77ba8b74e1f3f))

    -   **TruncationSize** (section [2.2.2.40.1](#Section_b672685019324420b0a22a892d97043c))

    -   **AllOrNone** (section [2.2.2.3.1](#Section_a0679789f0c149ca95b2348b8852027a))

    -   **Preview** (section [2.2.2.35.3](#Section_f00efde43d9346b1945a20346b53697f))

-   **Location** (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0))

    -   **DisplayName** (section [2.2.2.22.3](#Section_7497e46cce6c44b9aa7dc64951188dad))

    -   **Annotation** (section [2.2.2.6](#Section_68b6b09643a745cf9f61492334efaced))

    -   **Street** (section [2.2.2.38](#Section_bbf2888e23344087afa54551e2d400ce))

    -   **City** (section [2.2.2.13](#Section_00241f454bbf461880f04e74584121e0))

    -   **State** (section [2.2.2.36](#Section_efbda0bb756f4ecdace411bc5477dec0))

    -   **Country** (section [2.2.2.19](#Section_41cd8a0438b1493bb0bdb600429bc531))

    -   **PostalCode** (section [2.2.2.34](#Section_bfa47960d8844de299a42023647f5b41))

    -   **Latitude** (section [2.2.2.27](#Section_3c07ac1c46a44b89a5eff5abc30b297c))

    -   **Longitude** (section [2.2.2.30](#Section_7f62f1e593be4675b389b63903c5d472))

    -   **Accuracy** (section [2.2.2.1](#Section_13318ff160fe435c81d86a0a6ac92863))

    -   **Altitude** (section [2.2.2.4](#Section_cd8db29335c8457b867cdb2ba10cda59))

    -   **AltitudeAccuracy** (section [2.2.2.5](#Section_6b87364e7cda485691298775159a61aa))

    -   **LocationUri** (section [2.2.2.29](#Section_8256aa9c53b4427f9f95d3ae17f38c02))

##### MeetingResponse

The request message for the **MeetingResponse** command can include the following elements:

-   **Body** (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de))

    -   **Type** (section [2.2.2.41.1](#Section_02747a2d938143f499a9e854c6422901))

    -   **Data** (section [2.2.2.20.1](#Section_b673d5ba6afe466d97722a0ceaa49c3a))

##### Search

The request message for the **Search** command can include the following elements:

-   **BodyPreference** (section [2.2.2.12](#Section_793666561b7e4564a930ffb6fa85d283))

    -   **Type** (section [2.2.2.41.4](#Section_5a816c90c5fe4b4299ac5f85c7f3efb6))

    ```{=html}
    <!-- -->
    ```
    -   **TruncationSize** (section [2.2.2.40.2](#Section_acaa68c410fc403390cc45a5849b196d))

    ```{=html}
    <!-- -->
    ```
    -   **AllOrNone** (section [2.2.2.3.2](#Section_ca562432dee14a4da5fe9e6c20a3ced7))

    ```{=html}
    <!-- -->
    ```
    -   **Preview** (section [2.2.2.35.4](#Section_8932fcdb468c40e69aee380f78736019))

-   **BodyPartPreference** (section [2.2.2.11](#Section_6d9d09fd2ebc4570ba5cd6af546b3b12))

    -   **Type** (section [2.2.2.41.3](#Section_7044e1712c5945e98de77ba8b74e1f3f))

    ```{=html}
    <!-- -->
    ```
    -   **TruncationSize** (section [2.2.2.40.1](#Section_b672685019324420b0a22a892d97043c))

    ```{=html}
    <!-- -->
    ```
    -   **AllOrNone** (section [2.2.2.3.1](#Section_a0679789f0c149ca95b2348b8852027a))

    ```{=html}
    <!-- -->
    ```
    -   **Preview** (section [2.2.2.35.3](#Section_f00efde43d9346b1945a20346b53697f))

The **BodyPartPreference** element is only supported in a **Search** command request when the **ConversationId** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.35.2) is also included.

##### SmartForward

The request message for the **SmartForward** command can include the following elements:

-   **Body** (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de))

    -   **Type** (section [2.2.2.41.1](#Section_02747a2d938143f499a9e854c6422901))

    -   **Data** (section [2.2.2.20.1](#Section_b673d5ba6afe466d97722a0ceaa49c3a))

##### Sync

The request message for the **Sync** command can include the following elements:

-   **BodyPreference** (section [2.2.2.12](#Section_793666561b7e4564a930ffb6fa85d283))

    -   **Type** (section [2.2.2.41.4](#Section_5a816c90c5fe4b4299ac5f85c7f3efb6))

    ```{=html}
    <!-- -->
    ```
    -   **TruncationSize** (section [2.2.2.40.2](#Section_acaa68c410fc403390cc45a5849b196d))

    ```{=html}
    <!-- -->
    ```
    -   **AllOrNone** (section [2.2.2.3.2](#Section_ca562432dee14a4da5fe9e6c20a3ced7))

    ```{=html}
    <!-- -->
    ```
    -   **Preview** (section [2.2.2.35.4](#Section_8932fcdb468c40e69aee380f78736019))

-   **BodyPartPreference** (section [2.2.2.11](#Section_6d9d09fd2ebc4570ba5cd6af546b3b12))

    -   **Type** (section [2.2.2.41.3](#Section_7044e1712c5945e98de77ba8b74e1f3f))

    ```{=html}
    <!-- -->
    ```
    -   **TruncationSize** (section [2.2.2.40.1](#Section_b672685019324420b0a22a892d97043c))

    ```{=html}
    <!-- -->
    ```
    -   **AllOrNone** (section [2.2.2.3.1](#Section_a0679789f0c149ca95b2348b8852027a))

    ```{=html}
    <!-- -->
    ```
    -   **Preview** (section [2.2.2.35.3](#Section_f00efde43d9346b1945a20346b53697f))

```{=html}
<!-- -->
```
-   **InstanceId** (section [2.2.2.25](#Section_f684d282fb7e4326b64c1b8f68afc2ed))

-   **Location** (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0))

    -   **DisplayName** (section [2.2.2.22.3](#Section_7497e46cce6c44b9aa7dc64951188dad))

    -   **Annotation** (section [2.2.2.6](#Section_68b6b09643a745cf9f61492334efaced))

    -   **Street** (section [2.2.2.38](#Section_bbf2888e23344087afa54551e2d400ce))

    -   **City** (section [2.2.2.13](#Section_00241f454bbf461880f04e74584121e0))

    -   **State** (section [2.2.2.36](#Section_efbda0bb756f4ecdace411bc5477dec0))

    -   **Country** (section [2.2.2.19](#Section_41cd8a0438b1493bb0bdb600429bc531))

    -   **PostalCode** (section [2.2.2.34](#Section_bfa47960d8844de299a42023647f5b41))

    -   **Latitude** (section [2.2.2.27](#Section_3c07ac1c46a44b89a5eff5abc30b297c))

    -   **Longitude** (section [2.2.2.30](#Section_7f62f1e593be4675b389b63903c5d472))

    -   **Accuracy** (section [2.2.2.1](#Section_13318ff160fe435c81d86a0a6ac92863))

    -   **Altitude** (section [2.2.2.4](#Section_cd8db29335c8457b867cdb2ba10cda59))

    -   **AltitudeAccuracy** (section [2.2.2.5](#Section_6b87364e7cda485691298775159a61aa))

    -   **LocationUri** (section [2.2.2.29](#Section_8256aa9c53b4427f9f95d3ae17f38c02))

-   **Attachments** (section [2.2.2.8](#Section_12f116075694421ab7d1e436da4da613))

    -   **Add** (section [2.2.2.2](#Section_d60d864c07304b20bd74cc0245d4cdc6))

        -   **ClientId** (section [2.2.2.14](#Section_760294dc8295419eace3700bfa7c7266))

        -   **Method** (section [2.2.2.31.1](#Section_ce0e5014f5b247f8ba377983dbb2a8d0))

        -   **ContentType** (section [2.2.2.18.1](#Section_5c9b829b1de9400086382525d17dd850))

        -   **Content** (section [2.2.2.15](#Section_81bede41154743ef89f0d85e0e0ed2c1))

        -   **DisplayName** (section [2.2.2.22.1](#Section_2ddce3aaee104aacb717cd3ec6506704))

        -   **ContentId** (section [2.2.2.16.1](#Section_8a4746a7756a414687d1270d1c3e2f4f))

        -   **ContentLocation** (section [2.2.2.17.1](#Section_0bf6e69606bc423f96f4e80a6ad1f438))

        -   **IsInline** (section [2.2.2.26.1](#Section_b427fb12e516406399c0c3f1a3a2cdcd))

    -   **Delete** (section [2.2.2.21](#Section_b9f6e6cdfe714014a148cb21694f9a80))

        -   **FileReference** (section [2.2.2.24.2](#Section_5a6ef0d3d72640b997b79e6dbba2c59d))

### Timer Events

None.

### Other Local Events

None.

## Server Details

### Abstract Data Model

None.

### Timers

None.

### Initialization

None.

### Higher-Layer Triggered Events

None.

### Message Processing Events and Sequencing Rules

#### Validating XML

When the server receives an **ItemOperations**, **Search**, or **Sync** command, it SHOULD check any of the XML elements specified in section [2.2.2](#Section_05bfab44252f474b89b87ba6d60237e0) that are present in the command\'s XML body to ensure they comply with the requirements regarding data type, number of instances, order, and placement in the XML hierarchy. Unless specified in the following table, if an element does not meet the requirements specified for that element, the server SHOULD return protocol status error 2 for an **ItemOperations** command (as specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10) or a **Search** command (as specified in \[MS-ASCMD\] section 2.2.1.16), and protocol status error 6 for a **Sync** command (as specified in \[MS-ASCMD\] section 2.2.1.21).

  ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Element name                                                                         Condition                                                                                            Protocol Status Error
  ------------------------------------------------------------------------------------ ---------------------------------------------------------------------------------------------------- ---------------------------------------------------------------------------------------------------------------------------------------------
  **BodyPreference** (section [2.2.2.12](#Section_793666561b7e4564a930ffb6fa85d283))   Child elements are not in the correct order.                                                         4 (for **Sync** command)

  **BodyPreference**                                                                   Multiple **BodyPreference** elements are present with the same value in the **Type** child element   Server SHOULD return 4 (for **Sync** command), but MAY return an [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) error 500.[\<1\>](\l)

  **AllOrNone** (section [2.2.2.3.2](#Section_ca562432dee14a4da5fe9e6c20a3ced7))       The **AllOrNone** element is not of type **boolean**.                                                4 (for **Sync** command)

  **AllOrNone**                                                                        Multiple **AllOrNone** elements in a single **BodyPreference** element.                              4 (for **Sync** command)
  ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

#### Commands

The following table lists the commands that use the XML elements specified by this protocol.

  ---------------------------------------------------------------------------------------------------
  Command              Description
  -------------------- ------------------------------------------------------------------------------
  **ItemOperations**   Retrieves an item from the server.

  **Search**           Searches the server for items that match the specified criteria.

  **Sync**             Synchronizes changes in a collections set between the client and the server.
  ---------------------------------------------------------------------------------------------------

The server SHOULD process commands as specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a). The server SHOULD modify responses based on the elements specified in section [2.2.2](#Section_05bfab44252f474b89b87ba6d60237e0) as specified for each element.

##### ItemOperations

The response message for the **ItemOperations** command can include the following elements:

-   **Attachments** (section [2.2.2.8](#Section_12f116075694421ab7d1e436da4da613))

    -   **Attachment** (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda))

        -   **DisplayName** (section [2.2.2.22.2](#Section_b9ac5dbd51a6489c825d2c9f3c9bf41c))

        -   **FileReference** (section [2.2.2.24.1](#Section_9f86ef66b7ed48ad909f15c0e338fac8))

        -   **Method** (section [2.2.2.31.2](#Section_a1408a2d517848958103e66b435432be))

        -   **EstimatedDataSize** (section [2.2.2.23.1](#Section_6d5635c427da46c6b31dd4caea1df7f9))

        -   **ContentId** (section [2.2.2.16.2](#Section_be51de6eeb8341fbb79ca02d762c2ed0))

        -   **ContentLocation** (section [2.2.2.17.2](#Section_fe8bead0e57c4948ad5d68867e4c3f76))

        -   **IsInline** (section [2.2.2.26.2](#Section_c7e6d9c8b69a4e0c95769e55de793b65))

        -   **email2:UmAttDuration** ([\[MS-ASEMAIL\]](%5bMS-ASEMAIL%5d.pdf#Section_f3d27369e0f54164aa5e9b1abda16f5f) section 2.2.2.81)

        -   **email2:UmAttOrder** (\[MS-ASEMAIL\] section 2.2.2.82)

-   **Body** (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de))

    -   **Type** (section [2.2.2.41.1](#Section_02747a2d938143f499a9e854c6422901))

    -   **EstimatedDataSize** (section [2.2.2.23.2](#Section_108e3cbcc8b94b77a088e88c7bb4a8aa))

    -   **Truncated** (section [2.2.2.39.1](#Section_cada9eae58664657b11ea0feac6804e7))

    -   **Data** (section [2.2.2.20.1](#Section_b673d5ba6afe466d97722a0ceaa49c3a))

    -   **itemoperations:Part** (section [2.2.2.33](#Section_2e08aa0ad12e4b9ab3979e32ef4186b5))

    -   **Preview** (section [2.2.2.35.1](#Section_47ef04acfcfb4f0e81ec81d6ff10108f))

-   **BodyPart** (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5))

    -   **Status** (section [2.2.2.37](#Section_e1ec362012314d11b57752bb6a1d77be))

    -   **Type** (section [2.2.2.41.2](#Section_84a14e369b164757aa6566dbaa4c1d62))

    -   **EstimatedDataSize** (section [2.2.2.23.3](#Section_32c5e925fd624b7d8ffaa7d1199fe603))

    -   **Truncated** (section [2.2.2.39.2](#Section_eb254c9d777a4156adb67f6d1576f5c6))

    -   **Data** (section [2.2.2.20.2](#Section_8591cbbf434c4729bc3abf4f6d90902b))

    -   **Preview** (section [2.2.2.35.2](#Section_f4ea50c9fae54229b0433c4e2295d096))

```{=html}
<!-- -->
```
-   **ContentType** (section [2.2.2.18.2](#Section_735e38c609d04e2ca5fbe1d2394bd808))

-   **Location** (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0))

    -   **DisplayName** (section [2.2.2.22.3](#Section_7497e46cce6c44b9aa7dc64951188dad))

    -   **Annotation** (section [2.2.2.6](#Section_68b6b09643a745cf9f61492334efaced))

    -   **Street** (section [2.2.2.38](#Section_bbf2888e23344087afa54551e2d400ce))

    -   **City** (section [2.2.2.13](#Section_00241f454bbf461880f04e74584121e0))

    -   **State** (section [2.2.2.36](#Section_efbda0bb756f4ecdace411bc5477dec0))

    -   **Country** (section [2.2.2.19](#Section_41cd8a0438b1493bb0bdb600429bc531))

    -   **PostalCode** (section [2.2.2.34](#Section_bfa47960d8844de299a42023647f5b41))

    -   **Latitude** (section [2.2.2.27](#Section_3c07ac1c46a44b89a5eff5abc30b297c))

    -   **Longitude** (section [2.2.2.30](#Section_7f62f1e593be4675b389b63903c5d472))

    -   **Accuracy** (section [2.2.2.1](#Section_13318ff160fe435c81d86a0a6ac92863))

    -   **Altitude** (section [2.2.2.4](#Section_cd8db29335c8457b867cdb2ba10cda59))

    -   **AltitudeAccuracy** (section [2.2.2.5](#Section_6b87364e7cda485691298775159a61aa))

    -   **LocationUri** (section [2.2.2.29](#Section_8256aa9c53b4427f9f95d3ae17f38c02))

##### Search

The response message for the **Search** command can include the following elements:

-   **Attachments** (section [2.2.2.8](#Section_12f116075694421ab7d1e436da4da613))

    -   **Attachment** (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda))

        -   **DisplayName** (section [2.2.2.22.2](#Section_b9ac5dbd51a6489c825d2c9f3c9bf41c))

        -   **FileReference** (section [2.2.2.24.1](#Section_9f86ef66b7ed48ad909f15c0e338fac8))

        -   **Method** (section [2.2.2.31.2](#Section_a1408a2d517848958103e66b435432be))

        -   **EstimatedDataSize** (section [2.2.2.23.1](#Section_6d5635c427da46c6b31dd4caea1df7f9))

        -   **ContentId** (section [2.2.2.16.2](#Section_be51de6eeb8341fbb79ca02d762c2ed0))

        -   **ContentLocation** (section [2.2.2.17.2](#Section_fe8bead0e57c4948ad5d68867e4c3f76))

        -   **IsInline** (section [2.2.2.26.2](#Section_c7e6d9c8b69a4e0c95769e55de793b65))

        ```{=html}
        <!-- -->
        ```
        -   **email2:UmAttDuration** ([\[MS-ASEMAIL\]](%5bMS-ASEMAIL%5d.pdf#Section_f3d27369e0f54164aa5e9b1abda16f5f) section 2.2.2.81)

        ```{=html}
        <!-- -->
        ```
        -   **email2:UmAttOrder** (\[MS-ASEMAIL\] section 2.2.2.82)

-   **Body** (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de))

    -   **Type** (section [2.2.2.41.1](#Section_02747a2d938143f499a9e854c6422901))

    -   **EstimatedDataSize** (section [2.2.2.23.2](#Section_108e3cbcc8b94b77a088e88c7bb4a8aa))

    -   **Truncated** (section [2.2.2.39.1](#Section_cada9eae58664657b11ea0feac6804e7))

    ```{=html}
    <!-- -->
    ```
    -   **Data** (section [2.2.2.20.1](#Section_b673d5ba6afe466d97722a0ceaa49c3a))

    ```{=html}
    <!-- -->
    ```
    -   **Preview** (section [2.2.2.35.1](#Section_47ef04acfcfb4f0e81ec81d6ff10108f))

```{=html}
<!-- -->
```
-   **BodyPart** (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5))

    -   **Status** (section [2.2.2.37](#Section_e1ec362012314d11b57752bb6a1d77be))

    -   **Type** (section [2.2.2.41.2](#Section_84a14e369b164757aa6566dbaa4c1d62))

    -   **EstimatedDataSize** (section [2.2.2.23.3](#Section_32c5e925fd624b7d8ffaa7d1199fe603))

    -   **Truncated** (section [2.2.2.39.2](#Section_eb254c9d777a4156adb67f6d1576f5c6))

    -   **Data** (section [2.2.2.20.2](#Section_8591cbbf434c4729bc3abf4f6d90902b))

    -   **Preview** (section [2.2.2.35.2](#Section_f4ea50c9fae54229b0433c4e2295d096))

```{=html}
<!-- -->
```
-   **Location** (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0))

    -   **DisplayName** (section [2.2.2.22.3](#Section_7497e46cce6c44b9aa7dc64951188dad))

    -   **Annotation** (section [2.2.2.6](#Section_68b6b09643a745cf9f61492334efaced))

    -   **Street** (section [2.2.2.38](#Section_bbf2888e23344087afa54551e2d400ce))

    -   **City** (section [2.2.2.13](#Section_00241f454bbf461880f04e74584121e0))

    -   **State** (section [2.2.2.36](#Section_efbda0bb756f4ecdace411bc5477dec0))

    -   **Country** (section [2.2.2.19](#Section_41cd8a0438b1493bb0bdb600429bc531))

    -   **PostalCode** (section [2.2.2.34](#Section_bfa47960d8844de299a42023647f5b41))

    -   **Latitude** (section [2.2.2.27](#Section_3c07ac1c46a44b89a5eff5abc30b297c))

    -   **Longitude** (section [2.2.2.30](#Section_7f62f1e593be4675b389b63903c5d472))

    -   **Accuracy** (section [2.2.2.1](#Section_13318ff160fe435c81d86a0a6ac92863))

    -   **Altitude** (section [2.2.2.4](#Section_cd8db29335c8457b867cdb2ba10cda59))

    -   **AltitudeAccuracy** (section [2.2.2.5](#Section_6b87364e7cda485691298775159a61aa))

    -   **LocationUri** (section [2.2.2.29](#Section_8256aa9c53b4427f9f95d3ae17f38c02))

##### Sync

The response message for the **Sync** command can include the following elements:

-   **Attachments** (section [2.2.2.8](#Section_12f116075694421ab7d1e436da4da613))

    -   **Attachment** (section [2.2.2.7](#Section_359c0c61e90049d8a855d132a2e0edda))

        -   **DisplayName** (section [2.2.2.22.2](#Section_b9ac5dbd51a6489c825d2c9f3c9bf41c))

        -   **ClientId** (section [2.2.2.14](#Section_760294dc8295419eace3700bfa7c7266))

        -   **FileReference** (section [2.2.2.24.1](#Section_9f86ef66b7ed48ad909f15c0e338fac8))

        -   **Method** (section [2.2.2.31.2](#Section_a1408a2d517848958103e66b435432be))

        -   **EstimatedDataSize** (section [2.2.2.23.1](#Section_6d5635c427da46c6b31dd4caea1df7f9))

        -   **ContentId** (section [2.2.2.16.2](#Section_be51de6eeb8341fbb79ca02d762c2ed0))

        -   **ContentLocation** (section [2.2.2.17.2](#Section_fe8bead0e57c4948ad5d68867e4c3f76))

        -   **IsInline** (section [2.2.2.26.2](#Section_c7e6d9c8b69a4e0c95769e55de793b65))

        ```{=html}
        <!-- -->
        ```
        -   **email2:UmAttDuration** ([\[MS-ASEMAIL\]](%5bMS-ASEMAIL%5d.pdf#Section_f3d27369e0f54164aa5e9b1abda16f5f) section 2.2.2.81)

        ```{=html}
        <!-- -->
        ```
        -   **email2:UmAttOrder** (\[MS-ASEMAIL\] section 2.2.2.82)

    -   **Body** (section [2.2.2.9](#Section_4d2b12da9eff46f592e1e3780d1817de))

    -   **Type** (section [2.2.2.41.1](#Section_02747a2d938143f499a9e854c6422901))

    -   **EstimatedDataSize** (section [2.2.2.23.2](#Section_108e3cbcc8b94b77a088e88c7bb4a8aa))

    -   **Truncated** (section [2.2.2.39.1](#Section_cada9eae58664657b11ea0feac6804e7))

    ```{=html}
    <!-- -->
    ```
    -   **Data** (section [2.2.2.20.1](#Section_b673d5ba6afe466d97722a0ceaa49c3a))

    ```{=html}
    <!-- -->
    ```
    -   **Preview** (section [2.2.2.35.1](#Section_47ef04acfcfb4f0e81ec81d6ff10108f))

```{=html}
<!-- -->
```
-   **BodyPart** (section [2.2.2.10](#Section_e37bd54eebbc4efa88d674e9ef3544b5))

    -   **Status** (section [2.2.2.37](#Section_e1ec362012314d11b57752bb6a1d77be))

    ```{=html}
    <!-- -->
    ```
    -   **Type** (section [2.2.2.41.2](#Section_84a14e369b164757aa6566dbaa4c1d62))

    -   **EstimatedDataSize** (section [2.2.2.23.3](#Section_32c5e925fd624b7d8ffaa7d1199fe603))

    ```{=html}
    <!-- -->
    ```
    -   **Truncated** (section [2.2.2.39.2](#Section_eb254c9d777a4156adb67f6d1576f5c6))

    ```{=html}
    <!-- -->
    ```
    -   **Data** (section [2.2.2.20.2](#Section_8591cbbf434c4729bc3abf4f6d90902b))

    ```{=html}
    <!-- -->
    ```
    -   **Preview** (section [2.2.2.35.2](#Section_f4ea50c9fae54229b0433c4e2295d096))

-   **InstanceId** (section [2.2.2.25](#Section_f684d282fb7e4326b64c1b8f68afc2ed))

-   **NativeBodyType** (section [2.2.2.32](#Section_7a91ce24d4d1471c84723da9a59ebe1c))

```{=html}
<!-- -->
```
-   **Location** (section [2.2.2.28](#Section_b3701557630642938b87a796b4a3f9e0))

    -   **DisplayName** (section [2.2.2.22.3](#Section_7497e46cce6c44b9aa7dc64951188dad))

    -   **Annotation** (section [2.2.2.6](#Section_68b6b09643a745cf9f61492334efaced))

    -   **Street** (section [2.2.2.38](#Section_bbf2888e23344087afa54551e2d400ce))

    -   **City** (section [2.2.2.13](#Section_00241f454bbf461880f04e74584121e0))

    -   **State** (section [2.2.2.36](#Section_efbda0bb756f4ecdace411bc5477dec0))

    -   **Country** (section [2.2.2.19](#Section_41cd8a0438b1493bb0bdb600429bc531))

    -   **PostalCode** (section [2.2.2.34](#Section_bfa47960d8844de299a42023647f5b41))

    -   **Latitude** (section [2.2.2.27](#Section_3c07ac1c46a44b89a5eff5abc30b297c))

    -   **Longitude** (section [2.2.2.30](#Section_7f62f1e593be4675b389b63903c5d472))

    -   **Accuracy** (section [2.2.2.1](#Section_13318ff160fe435c81d86a0a6ac92863))

    -   **Altitude** (section [2.2.2.4](#Section_cd8db29335c8457b867cdb2ba10cda59))

    -   **AltitudeAccuracy** (section [2.2.2.5](#Section_6b87364e7cda485691298775159a61aa))

    -   **LocationUri** (section [2.2.2.29](#Section_8256aa9c53b4427f9f95d3ae17f38c02))

### Timer Events

None.

### Other Local Events

None.

# Protocol Examples

For examples of the **Search** command using this protocol, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 4.12. For examples of the **ItemOperations** command using this protocol, see \[MS-ASCMD\] section 4.10.2 and \[MS-ASCMD\] section 4.10.4. For examples of the **Sync** command using this protocol, see \[MS-ASCMD\] section 4.5.7.

# Security

## Security Considerations for Implementers

None.

## Index of Security Parameters

None.

# Appendix A: Full XML Schema

For ease of implementation, this section contains the contents of the AirSyncBase.xsd file, which represents the full [**XML schema**](#gt_bd0ce6f9-c350-4900-827e-951265294067) for this protocol. The additional files that this schema file requires to operate correctly are listed in the following table.

  ---------------------------------------------------------------------------------------------------------------------------------
  File name                           Defining specification
  ----------------------------------- ---------------------------------------------------------------------------------------------
  Email2.xsd                          [\[MS-ASEMAIL\]](%5bMS-ASEMAIL%5d.pdf#Section_f3d27369e0f54164aa5e9b1abda16f5f) section 6.2

  ItemOperations.xsd                  [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 6.22
  ---------------------------------------------------------------------------------------------------------------------------------

21. \<?xml version=\"1.0\" encoding=\"UTF-8\"?\>

    \<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:email2=\"Email2\"

    xmlns:itemoperations=\"ItemOperations\" xmlns=\"AirSyncBase\"

    targetNamespace=\"AirSyncBase\" elementFormDefault=\"qualified\"

    attributeFormDefault=\"unqualified\"\>

    \<xs:import namespace=\"Email2\" schemaLocation=\"Email2.xsd\"/\>

    \<xs:import namespace=\"ItemOperations\" schemaLocation=\"ItemOperations.xsd\"/\>

    \<xs:simpleType name=\"EmptyTag\"\>

    \<xs:restriction base=\"xs:string\"\>

    \<xs:maxLength value=\"0\"/\>

    \</xs:restriction\>

    \</xs:simpleType\>

    \<xs:element name=\"FileReference\" type=\"xs:string\"/\>

    \<xs:element name=\"BodyPreference\"\>

    \<xs:complexType\>

    \<xs:sequence\>

    \<xs:element name=\"Type\" type=\"xs:unsignedByte\"/\>

    \<xs:element name=\"TruncationSize\" type=\"xs:unsignedInt\" minOccurs=\"0\"/\>

    \<xs:element name=\"AllOrNone\" type=\"xs:boolean\" minOccurs=\"0\"/\>

    \<xs:element name=\"Preview\" minOccurs=\"0\"\>

    \<xs:simpleType\>

    \<xs:restriction base=\"xs:unsignedInt\"\>

    \<xs:minInclusive value=\"0\"/\>

    \<xs:maxInclusive value=\"255\"/\>

    \</xs:restriction\>

    \</xs:simpleType\>

    \</xs:element\>

    \</xs:sequence\>

    \</xs:complexType\>

    \</xs:element\>

    \<xs:element name=\"BodyPartPreference\"\>

    \<xs:complexType\>

    \<xs:sequence\>

    \<xs:element name=\"Type\"\>

    \<xs:simpleType\>

    \<xs:restriction base=\"xs:unsignedByte\"\>

    \<xs:minInclusive value=\"1\"/\>

    \<xs:maxInclusive value=\"4\"/\>

    \</xs:restriction\>

    \</xs:simpleType\>

    \</xs:element\>

    \<xs:element name=\"TruncationSize\" type=\"xs:unsignedInt\" minOccurs=\"0\"/\>

    \<xs:element name=\"AllOrNone\" type=\"xs:boolean\" minOccurs=\"0\"/\>

    \<xs:element name=\"Preview\" minOccurs=\"0\"\>

    \<xs:simpleType\>

    \<xs:restriction base=\"xs:unsignedInt\"\>

    \<xs:minInclusive value=\"0\"/\>

    \<xs:maxInclusive value=\"255\"/\>

    \</xs:restriction\>

    \</xs:simpleType\>

    \</xs:element\>

    \</xs:sequence\>

    \</xs:complexType\>

    \</xs:element\>

    \<xs:element name=\"Body\"\>

    \<xs:complexType\>

    \<xs:sequence\>

    \<xs:element name=\"Type\" type=\"xs:unsignedByte\"/\>

    \<xs:element name=\"EstimatedDataSize\" type=\"xs:unsignedInt\"

    minOccurs=\"0\"/\>

    \<xs:element name=\"Truncated\" type=\"xs:boolean\" minOccurs=\"0\"/\>

    \<xs:element name=\"Data\" type=\"xs:string\" minOccurs=\"0\"/\>

    \<xs:element name=\"Preview\" type=\"xs:string\" minOccurs=\"0\"/\>

    \<xs:element ref=\"itemoperations:Part\" minOccurs=\"0\"/\>

    \</xs:sequence\>

    \</xs:complexType\>

    \</xs:element\>

    \<xs:element name=\"BodyPart\"\>

    \<xs:complexType\>

    \<xs:sequence\>

    \<xs:element name=\"Status\"\>

    \<xs:simpleType\>

    \<xs:restriction base=\"xs:unsignedByte\"\>

    \<xs:enumeration value=\"1\"/\>

    \<xs:enumeration value=\"176\"/\>

    \</xs:restriction\>

    \</xs:simpleType\>

    \</xs:element\>

    \<xs:element name=\"Type\" type=\"xs:unsignedByte\"/\>

    \<xs:element name=\"EstimatedDataSize\" type=\"xs:unsignedInt\"/\>

    \<xs:element name=\"Truncated\" type=\"xs:boolean\" minOccurs=\"0\"/\>

    \<xs:element name=\"Data\" type=\"xs:string\" minOccurs=\"0\"/\>

    \<xs:element name=\"Preview\" type=\"xs:string\" minOccurs=\"0\"/\>

    \</xs:sequence\>

    \</xs:complexType\>

    \</xs:element\>

    \<xs:element name=\"Attachments\"\>

    \<xs:complexType\>

    \<xs:choice maxOccurs=\"unbounded\"\>

    \<xs:element name=\"Attachment\"\>

    \<xs:complexType\>

    \<xs:all\>

    \<xs:element name=\"DisplayName\" type=\"xs:string\" minOccurs=\"0\"/\>

    \<xs:element ref=\"FileReference\"/\>

    \<xs:element name=\"ClientId\" type=\"xs:string\" minOccurs=\"0\"/\>

    \<xs:element name=\"Method\" type=\"xs:unsignedByte\" minOccurs=\"0\"/\>

    \<xs:element name=\"EstimatedDataSize\" type=\"xs:unsignedInt\" minOccurs=\"0\"/\>

    \<xs:element name=\"ContentId\" type=\"xs:string\" minOccurs=\"0\"/\>

    \<xs:element name=\"ContentLocation\" type=\"xs:string\"

    minOccurs=\"0\"/\>

    \<xs:element name=\"IsInline\" type=\"xs:boolean\" minOccurs=\"0\"/\>

    \<xs:element ref=\"email2:UmAttDuration\" minOccurs=\"0\"/\>

    \<xs:element ref=\"email2:UmAttOrder\" minOccurs=\"0\"/\>

    \</xs:all\>

    \</xs:complexType\>

    \</xs:element\>

    \<xs:element minOccurs=\"0\" name=\"Add\"\>

    \<xs:complexType\>

    \<xs:all minOccurs=\"1\"\>

    \<xs:element name=\"ClientId\" type=\"xs:string\" minOccurs=\"1\"/\>

    \<xs:element name=\"Method\" type=\"xs:unsignedByte\" minOccurs=\"1\"/\>

    \<xs:element ref=\"ContentType\"/\>

    \<xs:element name=\"Content\" type=\"xs:string\" minOccurs=\"1\"/\>

    \<xs:element name=\"DisplayName\" type=\"xs:string\" minOccurs=\"1\"/\>

    \<xs:element name=\"ContentId\" type=\"xs:string\" minOccurs=\"0\" /\>

    \<xs:element name=\"ContentLocation\" type=\"xs:string\" minOccurs=\"0\"/\>

    \<xs:element name=\"IsInline\" minOccurs=\"0\"/\>

    \</xs:all\>

    \</xs:complexType\>

    \</xs:element\>

    \<xs:element minOccurs=\"0\" name=\"Delete\"\>

    \<xs:complexType\>

    \<xs:all minOccurs=\"1\"\>

    \<xs:element ref=\"FileReference\"/\>

    \</xs:all\>

    \</xs:complexType\>

    \</xs:element\>

    \</xs:choice\>

    \</xs:complexType\>

    \</xs:element\>

    \<xs:element name=\"NativeBodyType\" type=\"xs:unsignedByte\"/\>

    \<xs:element name=\"ContentType\" type=\"xs:string\" minOccurs=\"0\"/\>

    \<xs:element name=\"InstanceId\" type=\"xs:string\"/\>

    \<xs:element name=\"Location\"\>

    \<xs:complexType\>

    \<xs:all\>

    \<xs:element name=\"DisplayName\" type=\"xs:string\" minOccurs=\"0\"/\>

    \<xs:element name=\"Annotation\" type=\"xs:string\" minOccurs=\"0\"/\>

    \<xs:element name=\"Street\" type=\"xs:string\" minOccurs=\"0\"/\>

    \<xs:element name=\"City\" type=\"xs:string\" minOccurs=\"0\"/\>

    \<xs:element name=\"State\" type=\"xs:string\" minOccurs=\"0\"/\>

    \<xs:element name=\"Country\" type=\"xs:string\" minOccurs=\"0\"/\>

    \<xs:element name=\"PostalCode\" type=\"xs:string\" minOccurs=\"0\"/\>

    \<xs:element name=\"Latitude\" type=\"xs:double\" minOccurs=\"0\"/\>

    \<xs:element name=\"Longitude\" type=\"xs:double\" minOccurs=\"0\"/\>

    \<xs:element name=\"Accuracy\" type=\"xs:double\" minOccurs=\"0\"/\>

    \<xs:element name=\"Altitude\" type=\"xs:double\" minOccurs=\"0\"/\>

    \<xs:element name=\"AltitudeAccuracy\" type=\"xs:double\" minOccurs=\"0\"/\>

    \<xs:element name=\"LocationUri\" type=\"xs:string\" minOccurs=\"0\"/\>

    \</xs:all\>

    \</xs:complexType\>

    \</xs:element\>

    \<xs:group name=\"AllProps\"\>

    \<xs:sequence\>

    \<xs:choice maxOccurs=\"unbounded\"\>

    \<xs:element ref=\"Body\"/\>

    \<xs:element ref=\"BodyPart\"/\>

    \<xs:element ref=\"Attachments\"/\>

    \<xs:element ref=\"NativeBodyType\"/\>

    \</xs:choice\>

    \</xs:sequence\>

    \</xs:group\>

    \<xs:group name=\"TopLevelSchemaProps\"\>

    \<xs:sequence\>

    \<xs:choice maxOccurs=\"unbounded\"\>

    \<xs:element name=\"Body\" type=\"EmptyTag\"/\>

    \<xs:element name=\"BodyPart\" type=\"EmptyTag\"/\>

    \<xs:element name=\"Attachments\" type=\"EmptyTag\"/\>

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

-   Windows 8.1 operating system

-   Windows 10 operating system

-   Windows Server 2016 operating system

-   Windows 11 operating system

-   Microsoft Exchange Server Subscription Edition

Exceptions, if any, are noted in this section. If an update version, service pack or Knowledge Base (KB) number appears with a product name, the behavior changed in that update. The new behavior also applies to subsequent updates unless otherwise specified. If a product edition appears with the product version, behavior is different in that product edition.

Unless otherwise specified, any statement of optional behavior in this specification that is prescribed using the terms \"SHOULD\" or \"SHOULD NOT\" implies product behavior in accordance with the SHOULD or SHOULD NOT prescription. Unless otherwise specified, the term \"MAY\" implies that the product does not follow the prescription.

[\<1\> Section 3.2.5.1](\l): Exchange 2007 SP1 returns an [**HTTP**](#gt_d72f1494-4917-4e9e-a9fd-b8f1b2758dcd) error 500 instead of a **Status** value of 4 when multiple **BodyPreference** elements are present with the same value in the **Type** child element.

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
  [7](#Section_fb8342d762694ecabb432403e74561bf) Appendix B: Product Behavior   Updated list of supported products.   Major

  ------------------------------------------------------------------------------------------------------------------------------------

# Index

A

Abstract data model

[client](#abstract-data-model) 59

[server](#abstract-data-model-1) 62

[Applicability](#applicability-statement) 10

C

[Capability negotiation](#versioning-and-capability-negotiation) 10

[Change tracking](#change-tracking) 74

Client

[abstract data model](#abstract-data-model) 59

[higher-layer triggered events](#higher-layer-triggered-events) 59

[initialization](#initialization) 59

[other local events](#other-local-events) 62

[timer events](#timer-events) 62

[timers](#timers) 59

D

Data model - abstract

[client](#abstract-data-model) 59

[server](#abstract-data-model-1) 62

E

[Elements message](#elements) 11

Examples

[overview](#protocol-examples) 68

F

[Fields - vendor-extensible](#vendor-extensible-fields) 10

[Full XML schema](#appendix-a-full-xml-schema) 70

[XML Schema](#appendix-a-full-xml-schema) 70

G

[Glossary](#glossary) 7

[Groups message](#groups) 57

H

Higher-layer triggered events

[client](#higher-layer-triggered-events) 59

[server](#higher-layer-triggered-events-1) 63

I

[Implementer - security considerations](#security-considerations-for-implementers) 69

[Index of security parameters](#index-of-security-parameters) 69

[Informative references](#informative-references) 9

Initialization

[client](#initialization) 59

[server](#initialization-1) 63

[Introduction](#introduction) 7

M

Messages

[Elements](#elements) 11

[Groups](#groups) 57

[Namespaces](#namespaces) 11

[syntax](#message-syntax) 11

[transport](#transport) 11

N

[Namespaces message](#namespaces) 11

[Normative references](#normative-references) 8

O

Other local events

[client](#other-local-events) 62

[server](#other-local-events-1) 67

[Overview (synopsis)](#overview) 9

P

[Parameters - security index](#index-of-security-parameters) 69

[Preconditions](#prerequisitespreconditions) 10

[Prerequisites](#prerequisitespreconditions) 10

[Product behavior](#appendix-b-product-behavior) 73

R

[References](#references) 8

[informative](#informative-references) 9

[normative](#normative-references) 8

[Relationship to other protocols](#relationship-to-other-protocols) 9

S

Security

[implementer considerations](#security-considerations-for-implementers) 69

[parameter index](#index-of-security-parameters) 69

Server

[abstract data model](#abstract-data-model-1) 62

[higher-layer triggered events](#higher-layer-triggered-events-1) 63

[initialization](#initialization-1) 63

[other local events](#other-local-events-1) 67

[timer events](#timer-events-1) 67

[timers](#timers-1) 62

[Standards assignments](#standards-assignments) 10

T

Timer events

[client](#timer-events) 62

[server](#timer-events-1) 67

Timers

[client](#timers) 59

[server](#timers-1) 62

[Tracking changes](#change-tracking) 74

[Transport](#transport) 11

Triggered events - higher-layer

[client](#higher-layer-triggered-events) 59

[server](#higher-layer-triggered-events-1) 63

V

[Vendor-extensible fields](#vendor-extensible-fields) 10

[Versioning](#versioning-and-capability-negotiation) 10

X

[XML schema](#appendix-a-full-xml-schema) 70
