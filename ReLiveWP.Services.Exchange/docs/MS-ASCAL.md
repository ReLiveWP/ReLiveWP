**\[MS-ASCAL\]:**

**Exchange ActiveSync: Calendar Class Protocol**

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

  10/7/2011    9.1                Minor            Clarified the meaning of the technical content.

  1/20/2012    10.0               Major            Significantly changed the technical content.

  4/27/2012    10.1               Minor            Clarified the meaning of the technical content.

  7/16/2012    11.0               Major            Significantly changed the technical content.

  10/8/2012    11.1               Minor            Clarified the meaning of the technical content.

  2/11/2013    11.1               None             No changes to the meaning, language, or formatting of the technical content.

  7/26/2013    12.0               Major            Significantly changed the technical content.

  11/18/2013   12.0               None             No changes to the meaning, language, or formatting of the technical content.

  2/10/2014    12.0               None             No changes to the meaning, language, or formatting of the technical content.

  4/30/2014    13.0               Major            Significantly changed the technical content.

  7/31/2014    14.0               Major            Significantly changed the technical content.

  10/30/2014   14.1               Minor            Clarified the meaning of the technical content.

  5/26/2015    15.0               Major            Significantly changed the technical content.

  6/30/2015    15.0               None             No changes to the meaning, language, or formatting of the technical content.

  9/14/2015    16.0               Major            Significantly changed the technical content.

  6/9/2016     17.0               Major            Significantly changed the technical content.

  2/28/2017    18.0               Major            Significantly changed the technical content.

  4/18/2017    18.0               None             No changes to the meaning, language, or formatting of the technical content.

  7/24/2018    19.0               Major            Significantly changed the technical content.

  10/1/2018    20.0               Major            Significantly changed the technical content.

  11/17/2020   20.1               Minor            Clarified the meaning of the technical content.

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

[2.2.2.1 AllDayEvent [14](#alldayevent)](#alldayevent)

[2.2.2.2 AppointmentReplyTime [15](#appointmentreplytime)](#appointmentreplytime)

[2.2.2.3 Attendee [16](#attendee)](#attendee)

[2.2.2.4 Attendees [16](#attendees)](#attendees)

[2.2.2.5 AttendeeStatus [17](#attendeestatus)](#attendeestatus)

[2.2.2.6 AttendeeType [18](#attendeetype)](#attendeetype)

[2.2.2.7 Body [19](#body)](#body)

[2.2.2.7.1 Body (AirSyncBase Namespace) [19](#body-airsyncbase-namespace)](#body-airsyncbase-namespace)

[2.2.2.7.2 Body (Calendar Namespace) [20](#body-calendar-namespace)](#body-calendar-namespace)

[2.2.2.8 BodyTruncated [20](#bodytruncated)](#bodytruncated)

[2.2.2.9 BusyStatus [21](#busystatus)](#busystatus)

[2.2.2.10 CalendarType [22](#calendartype)](#calendartype)

[2.2.2.11 Categories [24](#categories)](#categories)

[2.2.2.12 Category [24](#category)](#category)

[2.2.2.13 ClientUid [25](#clientuid)](#clientuid)

[2.2.2.14 DayOfMonth [26](#dayofmonth)](#dayofmonth)

[2.2.2.15 DayOfWeek [26](#dayofweek)](#dayofweek)

[2.2.2.16 Deleted [27](#deleted)](#deleted)

[2.2.2.17 DisallowNewTimeProposal [28](#disallownewtimeproposal)](#disallownewtimeproposal)

[2.2.2.18 DtStamp [29](#dtstamp)](#dtstamp)

[2.2.2.19 Email [30](#email)](#email)

[2.2.2.20 EndTime [30](#endtime)](#endtime)

[2.2.2.21 Exception [31](#exception)](#exception)

[2.2.2.22 Exceptions [33](#exceptions)](#exceptions)

[2.2.2.23 ExceptionStartTime [33](#exceptionstarttime)](#exceptionstarttime)

[2.2.2.24 FirstDayOfWeek [34](#firstdayofweek)](#firstdayofweek)

[2.2.2.25 Interval [35](#interval)](#interval)

[2.2.2.26 IsLeapMonth [36](#isleapmonth)](#isleapmonth)

[2.2.2.27 Location [37](#location)](#location)

[2.2.2.28 MeetingStatus [37](#meetingstatus)](#meetingstatus)

[2.2.2.29 MonthOfYear [39](#monthofyear)](#monthofyear)

[2.2.2.30 Name [40](#name)](#name)

[2.2.2.31 NativeBodyType [40](#nativebodytype)](#nativebodytype)

[2.2.2.32 Occurrences [41](#occurrences)](#occurrences)

[2.2.2.33 OnlineMeetingConfLink [42](#onlinemeetingconflink)](#onlinemeetingconflink)

[2.2.2.34 OnlineMeetingExternalLink [42](#onlinemeetingexternallink)](#onlinemeetingexternallink)

[2.2.2.35 OrganizerEmail [43](#organizeremail)](#organizeremail)

[2.2.2.36 OrganizerName [44](#organizername)](#organizername)

[2.2.2.37 Recurrence [44](#recurrence)](#recurrence)

[2.2.2.37.1 Recurrence Patterns [45](#recurrence-patterns)](#recurrence-patterns)

[2.2.2.38 Reminder [47](#reminder)](#reminder)

[2.2.2.39 ResponseRequested [48](#responserequested)](#responserequested)

[2.2.2.40 ResponseType [48](#responsetype)](#responsetype)

[2.2.2.41 Sensitivity [49](#sensitivity)](#sensitivity)

[2.2.2.42 StartTime [50](#starttime)](#starttime)

[2.2.2.43 Subject [51](#subject)](#subject)

[2.2.2.44 Timezone [52](#timezone)](#timezone)

[2.2.2.45 Type [53](#type)](#type)

[2.2.2.46 UID [53](#uid)](#uid)

[2.2.2.47 Until [54](#until)](#until)

[2.2.2.48 WeekOfMonth [55](#weekofmonth)](#weekofmonth)

[2.2.3 Groups [56](#groups)](#groups)

[2.2.3.1 TopLevelSchemaProps [56](#toplevelschemaprops)](#toplevelschemaprops)

[3 Protocol Details [58](#protocol-details)](#protocol-details)

[3.1 Client Details [58](#client-details)](#client-details)

[3.1.1 Abstract Data Model [58](#abstract-data-model)](#abstract-data-model)

[3.1.2 Timers [58](#timers)](#timers)

[3.1.3 Initialization [58](#initialization)](#initialization)

[3.1.4 Higher-Layer Triggered Events [58](#higher-layer-triggered-events)](#higher-layer-triggered-events)

[3.1.4.1 Synchronizing Calendar Data Between Client and Server [58](#synchronizing-calendar-data-between-client-and-server)](#synchronizing-calendar-data-between-client-and-server)

[3.1.4.2 Searching a Server for Calendar Data [58](#searching-a-server-for-calendar-data)](#searching-a-server-for-calendar-data)

[3.1.4.3 Requesting Details for One or More Calendar Items [58](#requesting-details-for-one-or-more-calendar-items)](#requesting-details-for-one-or-more-calendar-items)

[3.1.4.4 Creating a New Meeting Request [58](#creating-a-new-meeting-request)](#creating-a-new-meeting-request)

[3.1.5 Message Processing Events and Sequencing Rules [59](#message-processing-events-and-sequencing-rules)](#message-processing-events-and-sequencing-rules)

[3.1.5.1 ItemOperations Command Request [59](#itemoperations-command-request)](#itemoperations-command-request)

[3.1.5.2 Search Command Request [59](#search-command-request)](#search-command-request)

[3.1.5.3 Sync Command Request [59](#sync-command-request)](#sync-command-request)

[3.1.5.3.1 Indicating Deleted Elements in Exceptions [59](#indicating-deleted-elements-in-exceptions)](#indicating-deleted-elements-in-exceptions)

[3.1.5.3.2 Omitting Ghosted Properties from a Sync Change Request [60](#omitting-ghosted-properties-from-a-sync-change-request)](#omitting-ghosted-properties-from-a-sync-change-request)

[3.1.6 Timer Events [60](#timer-events)](#timer-events)

[3.1.7 Other Local Events [60](#other-local-events)](#other-local-events)

[3.2 Server Details [60](#server-details)](#server-details)

[3.2.1 Abstract Data Model [60](#abstract-data-model-1)](#abstract-data-model-1)

[3.2.2 Timers [60](#timers-1)](#timers-1)

[3.2.3 Initialization [60](#initialization-1)](#initialization-1)

[3.2.4 Higher-Layer Triggered Events [61](#higher-layer-triggered-events-1)](#higher-layer-triggered-events-1)

[3.2.4.1 Synchronizing Calendar Data Between Client and Server [61](#synchronizing-calendar-data-between-client-and-server-1)](#synchronizing-calendar-data-between-client-and-server-1)

[3.2.4.2 Searching for Calendar Data [61](#searching-for-calendar-data)](#searching-for-calendar-data)

[3.2.4.3 Retrieving Details for One or More Calendar Items [61](#retrieving-details-for-one-or-more-calendar-items)](#retrieving-details-for-one-or-more-calendar-items)

[3.2.4.4 Creating Calendar Events when the StartTime Element or EndTime Element is Absent [61](#creating-calendar-events-when-the-starttime-element-or-endtime-element-is-absent)](#creating-calendar-events-when-the-starttime-element-or-endtime-element-is-absent)

[3.2.5 Message Processing Events and Sequencing Rules [61](#message-processing-events-and-sequencing-rules-1)](#message-processing-events-and-sequencing-rules-1)

[3.2.5.1 ItemOperations Command Response [62](#itemoperations-command-response)](#itemoperations-command-response)

[3.2.5.2 Search Command Response [62](#search-command-response)](#search-command-response)

[3.2.5.3 Sync Command Response [62](#sync-command-response)](#sync-command-response)

[3.2.5.3.1 Removing Exceptions [63](#removing-exceptions)](#removing-exceptions)

[3.2.5.3.2 Indicating Deleted Elements in Exceptions [63](#indicating-deleted-elements-in-exceptions-1)](#indicating-deleted-elements-in-exceptions-1)

[3.2.5.3.3 Omitting Ghosted Properties from a Sync Change Request [63](#omitting-ghosted-properties-from-a-sync-change-request-1)](#omitting-ghosted-properties-from-a-sync-change-request-1)

[3.2.6 Timer Events [64](#timer-events-1)](#timer-events-1)

[3.2.7 Other Local Events [64](#other-local-events-1)](#other-local-events-1)

[4 Protocol Examples [65](#protocol-examples)](#protocol-examples)

[4.1 Synchronizing Calendar Data [65](#synchronizing-calendar-data)](#synchronizing-calendar-data)

[4.2 Synchronizing Recurring Appointments with Exceptions [67](#synchronizing-recurring-appointments-with-exceptions)](#synchronizing-recurring-appointments-with-exceptions)

[4.3 Setting Attendee Status from the Server [68](#setting-attendee-status-from-the-server)](#setting-attendee-status-from-the-server)

[4.4 Creating Recurring Calendar Items [70](#creating-recurring-calendar-items)](#creating-recurring-calendar-items)

[4.5 Recurrence Patterns that Resolve to the Same Recurring Calendar Item [72](#recurrence-patterns-that-resolve-to-the-same-recurring-calendar-item)](#recurrence-patterns-that-resolve-to-the-same-recurring-calendar-item)

[5 Security [74](#security)](#security)

[5.1 Security Considerations for Implementers [74](#security-considerations-for-implementers)](#security-considerations-for-implementers)

[5.2 Index of Security Parameters [74](#index-of-security-parameters)](#index-of-security-parameters)

[6 Appendix A: Full XML Schema [75](#appendix-a-full-xml-schema)](#appendix-a-full-xml-schema)

[7 Appendix B: Product Behavior [80](#appendix-b-product-behavior)](#appendix-b-product-behavior)

[8 Change Tracking [81](#change-tracking)](#change-tracking)

[9 Index [82](#index)](#index)

# Introduction

The Exchange ActiveSync: Calendar Class Protocol enables the communication of calendar data between a mobile device and the server in the ActiveSync protocol.

Sections 1.5, 1.8, 1.9, 2, and 3 of this specification are normative. All other sections and examples in this specification are informative.

## Glossary

This document uses the following terms:

> []{#gt_c2354a51-451b-4296-88cd-3321c437d2c5 .anchor}**ghosted**: A property that is not deleted by the server if the element is not included in a Sync \<Change\> request message.
>
> []{#gt_72fbc9c5-8485-465c-8b46-64895c8d5102 .anchor}**Globally Routable User Agent URI (GRUU)**: A URI that identifies a user agent and is globally routable. A URI possesses a GRUU property if it is useable by any [**user agent client (UAC)**](#gt_e5f72a3f-9df4-47e1-b4ee-eda52237bafb) that is connected to the Internet, routable to a specific user agent instance, and long-lived.
>
> []{#gt_cbc56efc-e4f7-4b31-9e5f-9c44e3924d94 .anchor}**meeting**: An event with attendees.
>
> []{#gt_85d4db24-1560-4ac1-aa9b-6cd96f36c0e0 .anchor}**meeting request**: An instance of a Meeting Request object.
>
> []{#gt_34c00c47-5322-4cef-ae7e-bf04643b21bb .anchor}**organizer**: The owner or creator of a [**meeting**](#gt_cbc56efc-e4f7-4b31-9e5f-9c44e3924d94) or appointment.
>
> []{#gt_53dfe4f3-05d0-41aa-8217-ecd1962b340b .anchor}**recipient**: An entity that can receive email messages.
>
> []{#gt_4275047f-9935-46db-b9b8-8ca605d16649 .anchor}**recurrence pattern**: Information for a repeating event, such as the start and end time, the number of occurrences, and how occurrences are spaced, such as daily, weekly, or monthly.
>
> []{#gt_2325d666-e02f-49e4-afa5-3e896d672efe .anchor}**recurring series**: An event that repeats at specific intervals of time according to a recurrence pattern.
>
> []{#gt_8188a44f-a319-4d5c-9b3d-d65c4726045a .anchor}**reminder**: A generally user-visible notification that a specified time has been reached. A reminder is most commonly related to the beginning of a meeting or the due time of a task but it can be applied to any object type.
>
> []{#gt_94e97f15-2f1a-406f-a740-607bb97761ec .anchor}**resource**: Any component that a computer can access that can read, write, and process data. This includes internal components (such as a disk drive), a service, or an application running on and managed by the cluster on a network that is used to access a file.
>
> []{#gt_78bfb817-fde0-4756-9cae-7c68c5c962f5 .anchor}**tentative**: One of the possible values for the free/busy status on an appointment. A tentative status indicates that the user is tentatively booked during the appointment.
>
> []{#gt_433a4fb7-ef84-46b0-ab65-905f5e3a80b1 .anchor}**Uniform Resource Locator (URL)**: A string of characters in a standardized format that identifies a document or resource on the World Wide Web. The format is as specified in [\[RFC1738\]](https://go.microsoft.com/fwlink/?LinkId=90287).
>
> []{#gt_e5f72a3f-9df4-47e1-b4ee-eda52237bafb .anchor}**user agent client (UAC)**: A logical entity that creates a new request, and then uses the client transaction state machinery to send it. The role of [**UAC**](#gt_e5f72a3f-9df4-47e1-b4ee-eda52237bafb) lasts only for the duration of that transaction. In other words, if a piece of software initiates a request, it acts as a [**UAC**](#gt_e5f72a3f-9df4-47e1-b4ee-eda52237bafb) for the duration of that transaction. If it receives a request later, it assumes the role of a user agent server (UAS) for the processing of that transaction.
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

\[MS-OXOCAL\] Microsoft Corporation, \"[Appointment and Meeting Object Protocol](%5bMS-OXOCAL%5d.pdf#Section_09861fdec8e440289346e7c214cfdba1)\".

\[MS-SIPRE\] Microsoft Corporation, \"[Session Initiation Protocol (SIP) Routing Extensions](%5bMS-SIPRE%5d.pdf#Section_ab4ab24937964ed18cecf496d81a1a83)\".

\[RFC2119\] Bradner, S., \"Key words for use in RFCs to Indicate Requirement Levels\", BCP 14, RFC 2119, March 1997, [https://www.rfc-editor.org/info/rfc2119](https://go.microsoft.com/fwlink/?LinkId=90317)

\[XMLNS\] Bray, T., Hollander, D., Layman, A., et al., Eds., \"Namespaces in XML 1.0 (Third Edition)\", W3C Recommendation, December 2009, [https://www.w3.org/TR/2009/REC-xml-names-20091208/](https://go.microsoft.com/fwlink/?LinkId=191840)

\[XMLSCHEMA1/2\] Thompson, H., Beech, D., Maloney, M., and Mendelsohn, N., Eds., \"XML Schema Part 1: Structures Second Edition\", W3C Recommendation, October 2004, [https://www.w3.org/TR/2004/REC-xmlschema-1-20041028/](https://go.microsoft.com/fwlink/?LinkId=90607)

\[XMLSCHEMA2/2\] Biron, P., and Malhotra, A., Eds., \"XML Schema Part 2: Datatypes Second Edition\", W3C Recommendation, October 2004, [https://www.w3.org/TR/2004/REC-xmlschema-2-20041028/](https://go.microsoft.com/fwlink/?LinkId=90609)

\[XML\] World Wide Web Consortium, \"Extensible Markup Language (XML) 1.0 (Fourth Edition)\", W3C Recommendation 16 August 2006, edited in place 29 September 2006, [http://www.w3.org/TR/2006/REC-xml-20060816/](https://go.microsoft.com/fwlink/?LinkId=90598)

### Informative References

\[MS-OXPROTO\] Microsoft Corporation, \"[Exchange Server Protocols System Overview](%5bMS-OXPROTO%5d.pdf#Section_734ab967e43e425babe1974af56c0283)\".

## Overview

This protocol specifies an [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) representation of calendar data that is used for client and server communication as described in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a). The calendar data is included in protocol command requests when calendar data is sent from the client to the server, and is included in protocol command responses when calendar data is returned from the server to the client.

## Relationship to Other Protocols

This protocol specifies an [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) representation of calendar data that is used by the command requests and command responses that are described in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a). The protocol that controls the transmission of these commands between client and server is described in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d). The [**Wireless Application Protocol (WAP) Binary XML (WBXML)**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc), as described in [\[MS-ASWBXML\]](%5bMS-ASWBXML%5d.pdf#Section_39973eb11e404eb5ac7442781c5a33bc), is used to transmit the XML markup that constitutes the request body and the response body.

Some elements in the **Calendar** class support being [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). The use of ghosted properties is described in \[MS-ASCMD\] section 2.2.3.179.

All data types in this document conform to the data type definitions that are described in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3). Common [**XML schema**](#gt_bd0ce6f9-c350-4900-827e-951265294067) elements that are used by other classes are defined in [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c).

For conceptual background information and overviews of the relationships and interactions between this and other protocols, see [\[MS-OXPROTO\]](%5bMS-OXPROTO%5d.pdf#Section_734ab967e43e425babe1974af56c0283).

## Prerequisites/Preconditions

None.

## Applicability Statement

This protocol describes a set of [**XML elements**](#gt_a364f92c-0374-4568-b7f8-40bd74437dd5) that are used to communicate calendar data when using the commands described in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a). This set of elements is applicable when communicating calendar and [**meeting request**](#gt_85d4db24-1560-4ac1-aa9b-6cd96f36c0e0) information between a mobile device and a server. These elements are not applicable when communicating other types of information that are supported by the ActiveSync protocol.

## Versioning and Capability Negotiation

None.

## Vendor-Extensible Fields

None.

## Standards Assignments

None.

# Messages

## Transport

This protocol consists of a series of [**XML elements**](#gt_a364f92c-0374-4568-b7f8-40bd74437dd5) that are embedded inside of a command request or command response, as specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

The XML markup that constitutes the request body or the response body that is transmitted between the client and the server uses [**Wireless Application Protocol (WAP) Binary XML (WBXML)**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc), as specified in [\[MS-ASWBXML\]](%5bMS-ASWBXML%5d.pdf#Section_39973eb11e404eb5ac7442781c5a33bc).

## Message Syntax

The [**XML schema**](#gt_bd0ce6f9-c350-4900-827e-951265294067) for the Calendar namespace is described in section [6](#Section_a687889355dc44b4b9295b9e5465b340).

The markup that is used by this protocol MUST be well-formed [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85), as specified in [\[XML\]](https://go.microsoft.com/fwlink/?LinkId=90598).

### Namespaces

This specification defines and references various [**XML namespaces**](#gt_485f05b3-df3b-45ac-b8bf-d05f5d185a24) using the mechanisms specified in [\[XMLNS\]](https://go.microsoft.com/fwlink/?LinkId=191840). Although this specification associates a specific XML namespace prefix for each XML namespace that is used, the choice of any particular XML namespace prefix is implementation-specific and not significant for interoperability.

  ------------------------------------------------------------------------------------------------------------------------------------------------------
  Prefix           Namespace URI                          Reference
  ---------------- -------------------------------------- ----------------------------------------------------------------------------------------------
  calendar         **Calendar**                           

  airsyncbase      **AirSyncBase**                        [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c)

  airsync          **AirSync**                            [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21

  itemoperations   **ItemOperations**                     \[MS-ASCMD\] section 2.2.1.10

  search           **Search**                             \[MS-ASCMD\] section 2.2.1.16

  xs               **http://www.w3.org/2001/XMLSchema**   [\[XMLSCHEMA1/2\]](https://go.microsoft.com/fwlink/?LinkId=90607)
  ------------------------------------------------------------------------------------------------------------------------------------------------------

### Elements

Elements of the **Calendar** class are defined in two namespaces: **Calendar** and **AirSyncBase**. All **Calendar** class elements are specified in this document. However, elements defined in the **AirSyncBase** namespace are further specified in [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c).

The elements are defined in the **Calendar** namespace, except where indicated by the presence of a namespace prefix, as defined in section [2.2.1](#Section_0a58d4fdbbd34f2d92ebe8a00ad31126). A prefix is used for an element in the **Calendar** namespace only where necessary to disambiguate the element from another one of the same name.

The following elements are top-level elements of the **Calendar** class:

-   **Timezone** (section [2.2.2.44](#Section_a82384b7908a4a11930b7f091bb466ac))

-   **AllDayEvent** (section [2.2.2.1](#Section_deb50939a50a4e3dacd2f2031bc628df))

-   **airsyncbase:Body** (section [2.2.2.7.1](#Section_70161e943b504d208c4de3ca608f9dc8))

-   **calendar:Body** (section [2.2.2.7.2](#Section_9430068adab34acaa026e6b73f22570b))

-   **BodyTruncated** (section [2.2.2.8](#Section_4819149bd78e4172a2a02ebafdd2e086))

-   **BusyStatus** (section [2.2.2.9](#Section_9a37b42c67624d3e9e059c9221a1cd06))

-   **OrganizerName** (section [2.2.2.36](#Section_17689af0a79f4ed4bffaf4c1fca731f3))

-   **OrganizerEmail** (section [2.2.2.35](#Section_873950c9285c470e9e731a67cdc71a9a))

-   **DtStamp** (section [2.2.2.18](#Section_cc4110e7fbb74f01a0e3a29fb2b6d325))

-   **EndTime** (section [2.2.2.20](#Section_26046deef2af4a7ca9a647ebd43d6873))

-   **Location** (section [2.2.2.27](#Section_db0c360a786e45bd9f1c2932a3e5df2c))

-   **Reminder** (section [2.2.2.38](#Section_d9b081e091e14ec3a3174ebd0ccd4489))

-   **Sensitivity** (section [2.2.2.41](#Section_9c2b9eeccb794195876dd933c9043cbf))

-   **Subject** (section [2.2.2.43](#Section_04ce2d9e2d224df0aea6fa59597e731b))

-   **StartTime** (section [2.2.2.42](#Section_042bc0907eab40d79ebd77fd7c8f0559))

-   **ClientUid** (section [2.2.2.13](#Section_d22282da0cc54fbab4b45aef341a6c8e))

-   **UID** (section [2.2.2.46](#Section_8f1fb00ca15649e89f54478ecb0ce743))

-   **MeetingStatus** (section [2.2.2.28](#Section_c040515815e44f28a4ffe296644fef9f))

-   **Attendees** (section [2.2.2.4](#Section_8a8db399d4bc4742add06c76d4ada045))

-   **Categories** (section [2.2.2.11](#Section_4f34a8c7fc8d447a9f7cbcca0665814e))

-   **Recurrence** (section [2.2.2.37](#Section_dabc38cf7f144f518c88717dace42de5))

-   **Exceptions** (section [2.2.2.22](#Section_2fa6598590d44da79ddf7966531927d7))

-   **ResponseRequested** (section [2.2.2.39](#Section_9e43056042934d9e838c628137993f43))

-   **AppointmentReplyTime** (section [2.2.2.2](#Section_7d079aee5edd4c2696bad2b954034dbd))

-   **ResponseType** (section [2.2.2.40](#Section_00069422927f41adb06890007a1b0d45))

-   **DisallowNewTimeProposal** (section [2.2.2.17](#Section_f28bc518cfef4c9c9a2780777b446854))

-   **airsyncbase:NativeBodyType** (section [2.2.2.31](#Section_384c14bca8214e5da1d21eb3c58b845e))

-   **OnlineMeetingConfLink** (section [2.2.2.33](#Section_aa63e8872e0c487fa1a9d4466708a31b))

-   **OnlineMeetingExternalLink** (section [2.2.2.34](#Section_6367d2cb3dc847978b293f5b5fd7278f))

Except where otherwise specified in the following sections, each top-level element of the **Calendar** class is used in ActiveSync command requests and responses as follows:

-   As an optional child element of the **itemoperations:Schema** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.158) in **ItemOperations** command requests (\[MS-ASCMD\] section 2.2.1.10)

-   As an optional child element of the **itemoperations:Properties** element (\[MS-ASCMD\] section 2.2.3.139.2) in **ItemOperations** command responses (\[MS-ASCMD\] section 2.2.1.10)

-   As an optional child element of the **search:Properties** element (\[MS-ASCMD\] section 2.2.3.139.3) in **Search** command responses (\[MS-ASCMD\] section 2.2.1.16)

-   As an optional child element of the **airsync:ApplicationData** element (\[MS-ASCMD\] section 2.2.3.11) in **Sync** command requests (\[MS-ASCMD\] section 2.2.1.21)

-   As an optional child element of the **airsync:ApplicationData** element (\[MS-ASCMD\] section 2.2.3.11) in **Sync** command responses (\[MS-ASCMD\] section 2.2.1.21)

The following table summarizes the set of common [**XML schema**](#gt_bd0ce6f9-c350-4900-827e-951265294067) elements that are defined or used by this specification. XML schema elements that are specific to a particular operation are specified further in sections [3.1.5.1](#Section_7dbb7edf626a4b0ea944fcf2ebc37af4), [3.1.5.2](#Section_cc393fd783cb4f1db5e6ca892beac368), [3.1.5.3](#Section_7f6102708b9847ee851983f1b5cb9005), [3.2.5.1](#Section_ca9eeda6b99f4ebfa7d7da3f8e502d8b), [3.2.5.2](#Section_5d452e9dfe9747dfa3e4b3cc3795b0b4), and [3.2.5.3](#Section_da781554118d4a9aa400696035b777f2).

  -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Element name                                                                             Description
  ---------------------------------------------------------------------------------------- ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  **Timezone** (section 2.2.2.44)                                                          The time zone of the calendar item.

  **AllDayEvent** (section 2.2.2.1)                                                        Specifies whether the event represented by the calendar item or exception item spans the entire day.

  **airsyncbase:Body** (section 2.2.2.7.1)                                                 Specifies details about the body of a calendar item.

  **Body** (section 2.2.2.7.2)                                                             Contains the body of a calendar item that is retrieved from the server.

  **BodyTruncated** (section 2.2.2.8)                                                      Indicates whether the body of the calendar item was truncated when sent from the server.

  **BusyStatus** (section 2.2.2.9)                                                         Specifies whether the [**recipient**](#gt_53dfe4f3-05d0-41aa-8217-ecd1962b340b) is busy at the specified time.

  **OrganizerName** (section 2.2.2.36)                                                     The name of the user who created the calendar item.

  **OrganizerEmail** (section 2.2.2.35)                                                    The e-mail address of the user who created the calendar item.

  **DtStamp** (section 2.2.2.18)                                                           The date and time at which the calendar item was created or modified, or the date and time at which the exception item was created or modified.

  **EndTime** (section 2.2.2.20)                                                           The end time of the calendar item or exception item.

  **Location** (section 2.2.2.27)                                                          The place where the event specified by the calendar item or exception item occurs.

  **Reminder** (section 2.2.2.38)                                                          The number of minutes before the calendar item\'s start time to display a [**reminder**](#gt_8188a44f-a319-4d5c-9b3d-d65c4726045a) notice.

  **Sensitivity** (section 2.2.2.41)                                                       The recommended privacy policy for this calendar item or exception item.

  **Subject** (section 2.2.2.43)                                                           The subject of the calendar item or exception item.

  **StartTime** (section 2.2.2.42)                                                         The start time of the calendar item or exception item.

  **UID** (section 2.2.2.46)                                                               An ID that uniquely identifies a single event or [**recurring series**](#gt_2325d666-e02f-49e4-afa5-3e896d672efe).

  **ClientUid** (section 2.2.2.13)                                                         A random ID generated by the client when a calendar item is created.

  **MeetingStatus** (section 2.2.2.28)                                                     Indicates whether the event is a meeting or an appointment, whether the event is canceled or active, and whether the user was the [**organizer**](#gt_34c00c47-5322-4cef-ae7e-bf04643b21bb).

  **Attendees** (section 2.2.2.4)                                                          The collection of attendees for the calendar item.

  **Attendee** (section [2.2.2.3](#Section_af751263a16647dcb4e7ba411867bd22))              An attendee who is invited to the event.

  **Email** (section [2.2.2.19](#Section_21fdcd31695a4cbc9eb529dcd99bf748))                The e-mail address of the attendee.

  **Name** (section [2.2.2.30](#Section_042390494e354415bb45aa8f8bd9d34d))                 The name of the attendee.

  **AttendeeStatus** (section [2.2.2.5](#Section_9e773c231f154f8e95e7bc9cc6ec5592))        The attendee\'s acceptance status.

  **AttendeeType** (section [2.2.2.6](#Section_43fee1fc75ae411db2ea55bc290a1ff3))          Specifies whether the attendee is required, optional, or a [**resource**](#gt_94e97f15-2f1a-406f-a740-607bb97761ec).

  **Categories** (section 2.2.2.11)                                                        The collection of categories for the calendar item or exception item.

  **Category** (section [2.2.2.12](#Section_b9bffd66ee65462e8d979e14dd0d2b57))             A category that is assigned to the calendar item or exception item.

  **Recurrence** (section 2.2.2.37)                                                        The recurrence information for the calendar item.

  **Type** (section [2.2.2.45](#Section_d418d78b92114faf809fd4652dfd1f12))                 The type of the recurrence.

  **Occurrences** (section [2.2.2.32](#Section_7657d4cc0bac4c1c9b35786cc2c2a5ce))          The number of recurrences.

  **Interval** (section [2.2.2.25](#Section_841fec3360944475904627144a4c56fa))             The interval between recurrences.

  **WeekOfMonth** (section [2.2.2.48](#Section_2cbe8655957149cfaae78f0a29557b9d))          The week of the month for the recurrence.

  **DayOfWeek** (section [2.2.2.15](#Section_8c1edd6fb34e47599778fa4b211c9d9b))            The day of the week for the recurrence.

  **MonthOfYear** (section [2.2.2.29](#Section_692cb79e3c2d4ae9ae286079e9ef951e))          The month of the year for the recurrence.

  **Until** (section [2.2.2.47](#Section_6c21532bf3024f989eaf05e1ff723f4a))                The start time of the last instance of the recurring series.

  **DayOfMonth** (section [2.2.2.14](#Section_5f50e49cbe9c4c1794e91766af8fb8d2))           The day of the month of the recurrence.

  **CalendarType** (section [2.2.2.10](#Section_ca68b4ac49e8404ab291e82eb25ac885))         The calendar system used by the recurrence.

  **IsLeapMonth** (section [2.2.2.26](#Section_4642e794d9264cf3883e00bf01918001))          Specifies whether the recurrence of the appointment is to take place on the embolismic (leap) month.

  **FirstDayOfWeek** (section [2.2.2.24](#Section_438e160210424d6dbed977e2b0855da9))       Specifies which day is considered the first day of the calendar week for the recurrence.

  **Exceptions** (section 2.2.2.22)                                                        A collection of exceptions to the [**recurrence pattern**](#gt_4275047f-9935-46db-b9b8-8ca605d16649) of the calendar item.

  **Exception** (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436))            An exception to the calendar item\'s recurrence pattern.

  **Deleted** (section [2.2.2.16](#Section_3033158008e84a31801dc7c6c01ddd96))              Specifies whether the exception has been deleted.

  **ExceptionStartTime** (section [2.2.2.23](#Section_493af913219c475095b6d51fd0b76d98))   The original start time of the occurrence that the exception is replacing in the recurring series.

  **ResponseRequested** (section 2.2.2.39)                                                 Specifies whether a response to the meeting request is required.

  **AppointmentReplyTime** (section 2.2.2.2)                                               The date and time that the user responded to the meeting request or to the meeting exception request.

  **ResponseType** (section 2.2.2.40)                                                      The type of response made by the user to a meeting request.

  **DisallowNewTimeProposal** (section 2.2.2.17)                                           Specifies whether recipients of the meeting request can propose a new time for the meeting.

  **airsyncbase:NativeBodyType** (section 2.2.2.31)                                        Specifies how the body text of the calendar item is stored on the server.

  **OnlineMeetingConfLink** (section 2.2.2.33)                                             A [**Globally Routable User Agent URI (GRUU)**](#gt_72fbc9c5-8485-465c-8b46-64895c8d5102) ([\[MS-SIPRE\]](%5bMS-SIPRE%5d.pdf#Section_ab4ab24937964ed18cecf496d81a1a83)) for an online meeting.

  **OnlineMeetingExternalLink** (section 2.2.2.34)                                         A [**Uniform Resource Locator (URL)**](#gt_433a4fb7-ef84-46b0-ab65-905f5e3a80b1) for an online meeting.
  -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

#### AllDayEvent

As a top-level element of the **Calendar** class, the **AllDayEvent** element is an optional element that specifies whether the event represented by the calendar item spans the entire day. It is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

As an optional child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the **AllDayEvent** element specifies whether the event represented by the exception runs for the entire day. A command request or response has a maximum of one **AllDayEvent** child element per **Exception** element. If the **AllDayEvent** element is not specified as a child element of an **Exception** element, the value of the **AllDayEvent** element is assumed to be the same as the value of the top-level **AllDayEvent** element.

The **AllDayEvent** element is defined as an element in the **Calendar** namespace. The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8. The value of the **AllDayEvent** element MUST be one of the values listed in the following table.

  -----------------------------------------------------------------------
  Value              Meaning
  ------------------ ----------------------------------------------------
  0                  Is not an all-day event.

  1                  Is an all-day event.
  -----------------------------------------------------------------------

An item marked as an all-day event is understood to begin on midnight of the specified day and to end on midnight of the next day.

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

When protocol version 16.0 or 16.1 is used, the **AllDayEvent** element affects behavior as follows.

-   If a client includes an **Add** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.7.2) or a **Change** element (\[MS-ASCMD\] section 2.2.3.24) in a **Sync** command request (\[MS-ASCMD\] section 2.2.1.21) with **AllDayEvent** set to 1, the client MUST NOT include the **TimeZone** element (section [2.2.2.44](#Section_a82384b7908a4a11930b7f091bb466ac)) and MUST set the **StartTime** (section [2.2.2.42](#Section_042bc0907eab40d79ebd77fd7c8f0559)), **EndTime** (section [2.2.2.20](#Section_26046deef2af4a7ca9a647ebd43d6873)), and **Until** (section [2.2.2.47](#Section_6c21532bf3024f989eaf05e1ff723f4a)) elements to values that have no time component.

-   If the server includes an **Add** element or a **Change** element in a **Sync** command response with **AllDayEvent** set to 1, the server will not include the **TimeZone** element. In this case, a client SHOULD interpret this event to be at the given date(s) regardless of the time zone used.

-   When a client edits an exception item, the **AllDayEvent** element of the exception MUST match the value on the master item of the [**recurring series**](#gt_2325d666-e02f-49e4-afa5-3e896d672efe).

#### AppointmentReplyTime

As a top-level element of the **Calendar** class, the **AppointmentReplyTime** element is an optional element that specifies the date and time that the current user responded to the meeting request.

As an optional child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the **AppointmentReplyTime** element specifies the date and time that the user responded to the meeting request exception.

The **AppointmentReplyTime** element is defined as an element in the **Calendar** namespace. The value of this element is a **string** data type, represented as a **Compact DateTime** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.2).

A command request MUST NOT include the **AppointmentReplyTime** element, either as a top-level element or as a child element of the **Exception** element.

A command response has a maximum of one top-level **AppointmentReplyTime** element per response, and a maximum of one **AppointmentReplyTime** child element per **Exception** element.

The top-level **AppointmentReplyTime** element can be ghosted. For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

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

#### Attendee

The **Attendee** element is an optional **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that specifies an attendee who is invited to the event. It is a child element of the **Attendees** element (section [2.2.2.4](#Section_8a8db399d4bc4742add06c76d4ada045)) and is defined as an element in the **Calendar** namespace.

The **Attendee** element can have the following child elements:

-   **Email** (section [2.2.2.19](#Section_21fdcd31695a4cbc9eb529dcd99bf748)): One instance of this element is required.

-   **Name** (section [2.2.2.30](#Section_042390494e354415bb45aa8f8bd9d34d)): One instance of this element is required.

-   **AttendeeStatus** (section [2.2.2.5](#Section_9e773c231f154f8e95e7bc9cc6ec5592)): This element is optional.

-   **AttendeeType** (section [2.2.2.6](#Section_43fee1fc75ae411db2ea55bc290a1ff3)): This element is optional.

-   **ProposedStartTime** ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.141): This element is optional.

-   **ProposedEndTime** (\[MS-ASCMD\] section 2.2.3.140): This element is optional.

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

#### Attendees

As a top-level element of the **Calendar** class, the **Attendees** element is an optional **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that specifies the collection of attendees for the calendar item. It is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

As an optional child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the **Attendees** element specifies the collection of attendees for the calendar item exception. The **Attendees** element is not supported by certain protocol versions as a child of the **Exception** element. See the details about protocol versions at the end of this section. A command request or response has a maximum of one **Attendees** child element per **Exception** element. If the **Attendees** element is not specified as a child element of the **Exception** element, the attendees for the calendar item exception are assumed to be the same as the value of the top-level **Attendees** element.

The **Attendees** element is defined as an element in the **Calendar** namespace.

The **Attendees** element can have the following child element:

-   **Attendee** (section [2.2.2.3](#Section_af751263a16647dcb4e7ba411867bd22)): This element is optional.

The top-level **Attendees** element can be ghosted. For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -------------------------------------------------------------------------------------
  Protocol version   Element support, top-level   Element support, child of Exception
  ------------------ ---------------------------- -------------------------------------
  2.5                Yes                          

  12.0               Yes                          

  12.1               Yes                          

  14.0               Yes                          Yes

  14.1               Yes                          Yes

  16.0               Yes                          Yes

  16.1               Yes                          Yes
  -------------------------------------------------------------------------------------

When protocol version 2.5, 12.0, or 12.1 is used, the **Attendees** element is not supported as a child element of the **Exception** element.

#### AttendeeStatus

The **AttendeeStatus** element is an optional child element of the **Attendee** element (section [2.2.2.3](#Section_af751263a16647dcb4e7ba411867bd22)) that specifies the attendee\'s acceptance status. It is defined as an element in the **Calendar** namespace.

A command request has a maximum of one **AttendeeStatus** element per **Attendee** element.

A command response has a maximum of one **AttendeeStatus** element per **Attendee** element.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The value of the **AttendeeStatus** element MUST be one of the values listed in the following table.

  -----------------------------------------------------------------------
  Value                Meaning
  -------------------- --------------------------------------------------
  0                    Response unknown

  2                    Tentative

  3                    Accept

  4                    Decline

  5                    Not responded
  -----------------------------------------------------------------------

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

The client MUST NOT include the **AttendeeStatus** element in a command request when protocol version 16.0 or 16.1 is used.

#### AttendeeType

The **AttendeeType** element is an optional child element of the **Attendee** element (section [2.2.2.3](#Section_af751263a16647dcb4e7ba411867bd22)) that specifies whether the attendee is required, optional, or a resource. It is defined as an element in the **Calendar** namespace.

A command response has a maximum of one **AttendeeType** element per **Attendee** element.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The value of the **AttendeeType** element MUST be one of the values specified in the following table.

  -----------------------------------------------------------------------
  Value                          Meaning
  ------------------------------ ----------------------------------------
  1                              Required

  2                              Optional

  3                              Resource
  -----------------------------------------------------------------------

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

When protocol version 16.0 or 16.1 is used, the **AttendeeType** element is required.

#### Body

The **Body** element is defined in the **Calendar** namespace, as specified in section [2.2.2.7.2](#Section_9430068adab34acaa026e6b73f22570b), for use by protocol version 2.5. It is defined in the **AirSyncBase** namespace, as specified in section [2.2.2.7.1](#Section_70161e943b504d208c4de3ca608f9dc8), for use by protocol versions 12.0, 12.1, 14.0, 14.1, 16.0 and 16.1.

##### Body (AirSyncBase Namespace)

As a top-level element of the **Calendar** class, the **airsyncbase:Body** element is an optional **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that specifies details about the body of a calendar item. It is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

As an optional child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the **airsyncbase:Body** element is a **container** (\[MS-ASDTYPE\] section 2.2) element that specifies the body text of the calendar item exception. A command request or response has a maximum of one **airsyncbase:Body** child element per **Exception** element.

The top-level **airsyncbase:Body** element can be ghosted. For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

The **airsyncbase:Body** element is defined as an element in the **AirSyncBase** namespace and is further specified in [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.9.

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

The **calendar:Body** element (section [2.2.2.7.2](#Section_9430068adab34acaa026e6b73f22570b)) is used instead of the **airsyncbase:Body** element with protocol version 2.5.

##### Body (Calendar Namespace)

The **Body** element is an optional element that contains the body of a calendar item that is retrieved from the server. This element is defined in the **Calendar** namespace as a child of the **airsync:ApplicationData** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.11) in **Sync** command requests and responses (\[MS-ASCMD\] section 2.2.1.21).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

A client can use the **airsync:Truncation** element, as specified in \[MS-ASCMD\] section 2.2.3.185, to request truncation of a calendar item body. This conserves space and reduces data traffic when synchronizing calendar items. The server sets the **BodyTruncated** element (section [2.2.2.8](#Section_4819149bd78e4172a2a02ebafdd2e086)) in the **Sync** response to indicate whether the body of the calendar item has actually been truncated.

When the client requests truncation, only the first part (or none) of each calendar item body is included in a synchronization. A complete calendar item cannot be retrieved after it has been synchronized with a truncated calendar item body.

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

The **airsyncbase:Body** element (section [2.2.2.7.1](#Section_70161e943b504d208c4de3ca608f9dc8)) is used instead of the **calendar:Body** element with all protocol versions except 2.5.

#### BodyTruncated

The **BodyTruncated** element is an optional element that indicates whether the body of the calendar item was truncated when sent from the server. This element is defined in the **Calendar** namespace as a child of the **airsync:ApplicationData** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.11) in **Sync** command responses (\[MS-ASCMD\] section 2.2.1.21).

The value of this element is a **boolean** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.1.

A value of 1 indicates that the calendar item body has been truncated by the server; a value of 0 (zero) indicates that the calendar item body has not been truncated.

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

#### BusyStatus

As a top-level element of the **Calendar** class, the **BusyStatus** element is an optional element that specifies whether the recipient is busy at the time of the meeting. It is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

As an optional child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the **BusyStatus** element specifies the busy status of the meeting organizer. A command request or response has a maximum of one **BusyStatus** child element per **Exception** element. If the **BusyStatus** element is not specified as a child element of an **Exception** element, the value of the **BusyStatus** element is assumed to be the same as the value of the top-level **BusyStatus** element.

The **BusyStatus** element is defined as an element in the **Calendar** namespace. The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The value of the **BusyStatus** element MUST be one of the values listed in the following table.

  -----------------------------------------------------------------------
  Value                Meaning
  -------------------- --------------------------------------------------
  0                    Free

  1                    Tentative

  2                    Busy

  3                    Out of Office

  4                    Working elsewhere
  -----------------------------------------------------------------------

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

When protocol version 2.5 is used, the **BusyStatus** element is required.

The value 4 (working elsewhere) is not supported in protocol versions 2.5, 12.0, 12.1, 14.0, and 14.1.

The value 4 (working elsewhere) is not supported in a command request in protocol versions 16.0 and 16.1.

#### CalendarType

The **CalendarType** element is a child element of the **Recurrence** element (section [2.2.2.37](#Section_dabc38cf7f144f518c88717dace42de5)) that specifies the calendar system used by the recurrence. It is defined as an element in the **Calendar** namespace.

A command request has a maximum of one **CalendarType** child element per **Recurrence** element when the **Type** element (section [2.2.2.45](#Section_d418d78b92114faf809fd4652dfd1f12)) value is 2, 3, 5, or 6.

A command response has a minimum of one **CalendarType** child element per **Recurrence** element when the **Type** element value is 2, 3, 5, or 6. Otherwise, this element is optional in command responses.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The value of the **CalendarType** element MUST be one of the values listed in the following table.

  -----------------------------------------------------------------------
  Value     Meaning
  --------- -------------------------------------------------------------
  0         Default

  1         Gregorian

  2         Gregorian (United States)

  3         Japanese Emperor Era

  4         Taiwan

  5         Korean Tangun Era

  6         Hijri (Arabic Lunar)

  7         Thai

  8         Hebrew Lunar

  9         Gregorian (Middle East French)

  10        Gregorian (Arabic)

  11        Gregorian (Transliterated English)

  12        Gregorian (Transliterated French)

  13        Reserved. MUST NOT be used.

  14        Japanese Lunar

  15        Chinese Lunar

  16        Saka Era. Reserved. MUST NOT be used.

  17        Chinese Lunar (Eto). Reserved. MUST NOT be used.

  18        Korean Lunar (Eto). Reserved. MUST NOT be used.

  19        Japanese Rokuyou Lunar. Reserved. MUST NOT be used.

  20        Korean Lunar

  21        Reserved. MUST NOT be used.

  22        Reserved. MUST NOT be used.

  23        Um al-Qura. Reserved. MUST NOT be used
  -----------------------------------------------------------------------

The server MAY[\<1\>](\l) return a value of 0 (Default) when a client specifies a value of 1 (Gregorian).

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

#### Categories

As a top-level element of the **Calendar** class, the **Categories** element is an optional **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that specifies a collection of categories assigned to the calendar item. It is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

As a child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the **Categories** element is an optional **container** (\[MS-ASDTYPE\] section 2.2) element that specifies the categories for the exception item. A command request or response has a maximum of one **Categories** child element per **Exception** element.

The **Categories** element is defined as an element in the **Calendar** namespace.

The **Categories** element can have the following child element:

-   **Category** (section [2.2.2.12](#Section_b9bffd66ee65462e8d979e14dd0d2b57))

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

The **Category** element is an optional child element of the **Categories** element (section [2.2.2.11](#Section_4f34a8c7fc8d447a9f7cbcca0665814e)) that specifies a category that is assigned to the calendar item or exception item. It is defined as an element in the **Calendar** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

A command request SHOULD include no more than 300 **Category** child elements per **Categories** element.

A command response SHOULD include no more than 300 **Category** child elements per **Categories** element.

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

#### ClientUid

The **ClientUid** element is an optional element in the command request that specifies a random ID generated by the client when the calendar item is created. It is defined as an element in the **Calendar** namespace and is used in command requests, as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

The **ClientUid** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7. The maximum length of the string is 300 characters.

This element, if present, SHOULD remain the same between client requests if the client is attempting to add the same event or [**recurring series**](#gt_2325d666-e02f-49e4-afa5-3e896d672efe). The server will use this value to detect if the item being added already exists on the user\'s calendar.

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

#### DayOfMonth

The **DayOfMonth** element is a child element of the **Recurrence** element (section [2.2.2.37](#Section_dabc38cf7f144f518c88717dace42de5)) that specifies the day of the month for the recurrence. It is defined as an element in the **Calendar** namespace.

A command request or response has a minimum of one **DayOfMonth** child element per **Recurrence** element when the value of the **Type** element (section [2.2.2.45](#Section_d418d78b92114faf809fd4652dfd1f12)) is either 2 or 5.

A command request or response has a maximum of one **DayOfMonth** child element per **Recurrence** element.

The value of the **DayOfMonth** element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8. The value of this element MUST be between 1 and 31.

The **DayOfMonth** element MUST be included in requests or responses when the **Type** element value is either 2 or 5. The **DayOfMonth** element MUST NOT be included in requests or responses when the **Type** element value is zero (0), 1, 3, or 6.

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

#### DayOfWeek

The **DayOfWeek** element is a child element of the **Recurrence** element (section [2.2.2.37](#Section_dabc38cf7f144f518c88717dace42de5)) that specifies the day of the week for the recurrence. It is defined as an element in the **Calendar** namespace.

A command request or response has a maximum of one **DayOfWeek** child element per **Recurrence** element.

The value of this element is an **unsignedShort** data type, as specified in [\[XMLSCHEMA2/2\]](https://go.microsoft.com/fwlink/?LinkId=90609).

The value of the **DayOfWeek** element MUST be either one of the values listed in the following table, or the sum of more than one of the values listed in the following table (in which case this task recurs on more than one day). The value of the **DayOfWeek** element MUST NOT be greater than 127.

  ----------------------------------------------------------------------------------------------
  Value   Meaning
  ------- --------------------------------------------------------------------------------------
  1       Sunday

  2       Monday

  4       Tuesday

  8       Wednesday

  16      Thursday

  32      Friday

  62      Weekdays

  64      Saturday

  65      Weekend days

  127     The last day of the month. Used as a special value in monthly or yearly recurrences.
  ----------------------------------------------------------------------------------------------

The **DayOfWeek** element MUST only be included in requests or responses when the **Type** element (section [2.2.2.45](#Section_d418d78b92114faf809fd4652dfd1f12)) value is 0 (zero), 1, 3, or 6.

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

#### Deleted

The **Deleted** element is an optional child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)) that specifies whether the exception to the calendar item has been deleted. It is defined as an element in the **Calendar** namespace.

A command request or response has a maximum of one **Deleted** child element per **Exception** element.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

An exception will be deleted when the **Deleted** element is included as a child element of the **Exception** element with a value of 1.

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

#### DisallowNewTimeProposal

The **DisallowNewTimeProposal** element is an optional element that specifies whether a meeting request recipient can propose a new time for the scheduled meeting. It is defined as an element in the **Calendar** namespace.

A command request is not required to include the **DisallowNewTimeProposal** element. If this element is not included in a command request, then the default value for this element is 0 (FALSE).

A command response contains one **DisallowNewTimeProposal** element per response.

The value of the **DisallowNewTimeProposal** element is a **boolean** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.1.

The **DisallowNewTimeProposal** element can be ghosted. For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

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

#### DtStamp

As a top-level element of the **Calendar** class, the **DtStamp** element is an optional element that specifies the date and time at which the calendar item was created or modified or the date and time at which the exception item was created or modified. It is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

As an optional child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the **DtStamp** element specifies the date and time that this exception was created. A command request or response has a maximum of one **DtStamp** child element per **Exception** element. If the **DtStamp** element is not specified as a child element of an **Exception** element, the value of the **DtStamp** element is assumed to be the same as the value of the top-level **DtStamp** element.

The **DtStamp** element is defined as an element in the **Calendar** namespace. The value of this element is a **string** data type, represented as a **Compact** **DateTime** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.2).

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

When protocol version 2.5 is used, the **DtStamp** element is required.

The client MUST NOT include the **DtStamp** element in command requests when protocol version 16.0 or 16.1 is used.

#### Email

The **Email** element is a required child element of the **Attendee** element (section [2.2.2.3](#Section_af751263a16647dcb4e7ba411867bd22)) that specifies the e-mail address of an attendee. It is defined as an element in the **Calendar** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

A command request or response has only **Email** child element per **Attendee** element.

The value of the **Email** element MAY be any arbitrary string. It is recommended that the string format adhere to the format specified in \[MS-ASDTYPE\] section 2.7.3.

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

#### EndTime

As a top-level element of the **Calendar** class, the **EndTime** element is an optional element that specifies the end time of the calendar item. The client SHOULD include the **EndTime** element in a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21). The **EndTime** element MUST be present in the response as a top-level element, even if the value of the **AllDayEvent** element (section [2.2.2.1](#Section_deb50939a50a4e3dacd2f2031bc628df)) is 1.

For details about server behavior when a calendar event is received that is missing either the **StartTime** element (section [2.2.2.42](#Section_042bc0907eab40d79ebd77fd7c8f0559)), the **EndTime** element, or both, see section [3.2.4.4](#Section_d36fecc392244c65b58db4ddb354e93e).

As an optional child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the **EndTime** element specifies the end time of the calendar item exception. A command request or response has a maximum of one **EndTime** child element per **Exception** element. If the **EndTime** element is not specified as a child element of the **Exception** element, the value of the **EndTime** element for the calendar item exception is assumed to be the same as the value of the top-level **EndTime** element.

The **EndTime** element is defined as an element in the **Calendar** namespace. The value of this element is a **string** data type represented as a **Compact DateTime** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.2).

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

When protocol version 2.5 is used, the **EndTime** element MUST be included in the request.

In protocol version 16.0 and 16.1, changing the end time of a [**recurring series**](#gt_2325d666-e02f-49e4-afa5-3e896d672efe) will delete any exceptions present on the calendar item.

#### Exception

The **Exception** element is an optional **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that specifies an exception to the calendar item\'s [**recurrence pattern**](#gt_4275047f-9935-46db-b9b8-8ca605d16649). It is a child element of the **Exceptions** element (section [2.2.2.22](#Section_2fa6598590d44da79ddf7966531927d7)) and is defined as an element in the **Calendar** namespace.

A command request or response has between zero and 256 **Exception** child elements per **Exceptions** element.

The **Exception** element can have the following child elements. Some of these elements are not supported by certain protocol versions as a child elements of the **Exception** element. See the details about protocol versions at the end of this section.

-   **Deleted** (section [2.2.2.16](#Section_3033158008e84a31801dc7c6c01ddd96)): This element is optional.

-   **ExceptionStartTime** (section [2.2.2.23](#Section_493af913219c475095b6d51fd0b76d98)): One instance of this element is required.

-   **Subject** (section [2.2.2.43](#Section_04ce2d9e2d224df0aea6fa59597e731b)): This element is optional.

-   **StartTime** (section [2.2.2.42](#Section_042bc0907eab40d79ebd77fd7c8f0559)): This element is optional.

-   **EndTime** (section [2.2.2.20](#Section_26046deef2af4a7ca9a647ebd43d6873)): This element is optional.

-   **airsyncbase:Body** (section [2.2.2.7.1](#Section_70161e943b504d208c4de3ca608f9dc8)): This element is optional.

-   **calendar:Body** (section [2.2.2.7.2](#Section_9430068adab34acaa026e6b73f22570b)): This element is optional.

-   **airsyncbase:Location** ([\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.28): This element is optional.

-   **calendar:Location** (section [2.2.2.27](#Section_db0c360a786e45bd9f1c2932a3e5df2c)): This element is optional.

-   **airsyncbase:Attachments** (\[MS-ASAIRS\] section 2.2.2.8): This element is optional.

-   **Categories** (section [2.2.2.11](#Section_4f34a8c7fc8d447a9f7cbcca0665814e)): This element is optional.

-   **Sensitivity** (section [2.2.2.41](#Section_9c2b9eeccb794195876dd933c9043cbf)): This element is optional.

-   **BusyStatus** (section [2.2.2.9](#Section_9a37b42c67624d3e9e059c9221a1cd06)): This element is optional.

-   **AllDayEvent** (section [2.2.2.1](#Section_deb50939a50a4e3dacd2f2031bc628df)): This element is optional.

-   **Reminder** (section [2.2.2.38](#Section_d9b081e091e14ec3a3174ebd0ccd4489)): This element is optional.

-   **DtStamp** (section [2.2.2.18](#Section_cc4110e7fbb74f01a0e3a29fb2b6d325)): This element is optional.

-   **UID** (section [2.2.2.46](#Section_8f1fb00ca15649e89f54478ecb0ce743)): This element is required.

-   **airsyncbase:InstanceId** (\[MS-ASAIRS\] section 2.2.2.25): This element is required.

-   **MeetingStatus** (section [2.2.2.28](#Section_c040515815e44f28a4ffe296644fef9f)): This element is optional.

-   **Attendees** (section [2.2.2.4](#Section_8a8db399d4bc4742add06c76d4ada045)): This element is optional.

-   **AppointmentReplyTime** (section [2.2.2.2](#Section_7d079aee5edd4c2696bad2b954034dbd)): This element is optional in command responses. It is not included in command requests.

-   **ResponseType** (section [2.2.2.40](#Section_00069422927f41adb06890007a1b0d45)): This element is optional in command responses. It is not included in command requests.

-   **OnlineMeetingConfLink** (section [2.2.2.33](#Section_aa63e8872e0c487fa1a9d4466708a31b)): This element is optional in command responses. It is not included in command requests.

-   **OnlineMeetingExternalLink** (section [2.2.2.34](#Section_6367d2cb3dc847978b293f5b5fd7278f)): This element is optional in command responses. It is not included in command requests.

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

The **calendar:Body** element and the **UID** element are supported as a child elements of the **Exception** element only when protocol version 2.5 is used. The **airsyncbase:Body** element is used instead of the **calendar:Body** element with protocol versions 12.0, 12.1, 14.0, 14.1, 16.0 and 16.1.

The following elements are supported as a child elements of the **Exception** element only when protocol version 14.0, 14.1, 16.0, or 16.1 is used: **Attendees**, **AppointmentReplyTime**, and **ResponseType**.

The following elements are supported as a child elements of the **Exception** element only when protocol version 14.1, 16.0 or 16.1 is used: **OnlineMeetingConfLink** and **OnlineMeetingExternalLink**.

The **ExceptionStartTime** element is a required child element of the **Exception** element only when protocol version 2.5, 12.0, 12.1, 14.0, or 14.1 is used.

The **InstanceId**, **airsyncbase:Location**, and **airsyncbase:Attachments** elements are supported as child elements of the **Exception** element only when protocol version 16.0 or 16.1 is used.

#### Exceptions

The **Exceptions** element is an optional **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that specifies a collection of exceptions to the [**recurrence pattern**](#gt_4275047f-9935-46db-b9b8-8ca605d16649) of the calendar item. It is defined as an element in the **Calendar** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

The **Exceptions** element can have the following child element:

-   **Exception** (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)): This element is optional.

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

The client MUST NOT include the **Exceptions** element in a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) to change an exception when protocol version 16.0 or 16.1 is used. Instead, the client includes the **airsyncbase:InstanceId** element ([\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.25) along with the **airsync:ServerId** element (\[MS-ASCMD\] section 2.2.3.166.8) to change an exception.

In protocol version 16.0 and 16.1, changing the recurrence pattern or the start/end times of a [**recurring series**](#gt_2325d666-e02f-49e4-afa5-3e896d672efe) will delete any exceptions present on the calendar item.

#### ExceptionStartTime

The **ExceptionStartTime** element is a required child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)) that specifies the original start time of the occurrence that the exception is replacing in the [**recurring series**](#gt_2325d666-e02f-49e4-afa5-3e896d672efe). It is defined as an element in the **Calendar** namespace.

A command request or response has only one **ExceptionStartTime** child element per **Exception** element.

The value of the **ExceptionStartTime** element is a **string** data type, represented as a **Compact** **DateTime** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.2).

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

  16.0                                

  16.1                                
  -----------------------------------------------------------------------

#### FirstDayOfWeek

The **FirstDayOfWeek** element is a child element of the **Recurrence** element (section [2.2.2.37](#Section_dabc38cf7f144f518c88717dace42de5)) that specifies which day is considered the first day of the calendar week for the recurrence. It is defined as an element in the **Calendar** namespace.

A command request has a maximum of one **FirstDayOfWeek** child element per **Recurrence** element. A command response has a maximum of one **FirstDayOfWeek** child element per **Recurrence** element.

This element disambiguates recurrences across localities that define a different starting day for the calendar week.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The value of the **FirstDayOfWeek** element MUST be one of the values listed in the following table.

  -----------------------------------------------------------------------
  Value                       Meaning
  --------------------------- -------------------------------------------
  0                           Sunday

  1                           Monday

  2                           Tuesday

  3                           Wednesday

  4                           Thursday

  5                           Friday

  6                           Saturday
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

#### Interval

The **Interval** element is an optional child element of the **Recurrence** element (section [2.2.2.37](#Section_dabc38cf7f144f518c88717dace42de5)) that specifies the interval between recurrences. It is defined as an element in the **Calendar** namespace.

A command request or response has a maximum of one **Interval** child element per **Recurrence** element.

The value of this element is an **unsignedShort** data type, as specified in [\[XMLSCHEMA2/2\]](https://go.microsoft.com/fwlink/?LinkId=90609), with a minimum value of 0[\<2\>](\l) and a maximum value of 999.

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

#### IsLeapMonth

The **IsLeapMonth** element is an optional child element of the **Recurrence** element (section [2.2.2.37](#Section_dabc38cf7f144f518c88717dace42de5)) that specifies whether the recurrence of the appointment takes place on the embolismic (leap) month. It is defined as an element in the **Calendar** namespace. A command request has a maximum of one **IsLeapMonth** child element per **Recurrence** element.

A command response has a maximum of one **IsLeapMonth** child element per **Recurrence** element.

This element only applies when the **CalendarType** element (section [2.2.2.10](#Section_ca68b4ac49e8404ab291e82eb25ac885)) specifies a calendar system that incorporates an embolismic (leap) month. Examples include lunisolar calendar systems such as Hebrew Lunar and Chinese Lunar. This element has no effect when specified in conjunction with the Gregorian calendar.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The value of the **IsLeapMonth** element MUST be one of the values listed in the following table.

  -----------------------------------------------------------------------
  Value                          Meaning
  ------------------------------ ----------------------------------------
  0                              False

  1                              True
  -----------------------------------------------------------------------

The default value of the **IsLeapMonth** element is 0 (FALSE).

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

#### Location

As a top-level element of the **Calendar** class, the **Location** element is an optional element that specifies the place where the event specified by the calendar item occurs. It is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

As an optional child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the **Location** element specifies the place where the event specified by the calendar item exception occurs. A command request or response has a maximum of one **Location** child element per **Exception** element. If the **Location** element is not specified as a child element of the **Exception** element, the value of the **Location** element for the exception is assumed to be the same as the value of the top-level **Location** element.

The **Location** element is defined as an element in the **Calendar** namespace. The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

The top-level **Location** element cannot be ghosted. For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

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

  16.0                                

  16.1                                
  -----------------------------------------------------------------------

The **airsyncbase:Location** element ([\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.28) is used instead of the **calendar:Location** element in protocol version 16.0 and 16.1.

#### MeetingStatus

As a top-level element of the **Calendar** class, the **MeetingStatus** element is an optional element that specifies whether the event is a meeting or an appointment, whether the event is canceled or active, and whether the user was the [**organizer**](#gt_34c00c47-5322-4cef-ae7e-bf04643b21bb). It is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

As an optional child element of the **Exception** element, the **MeetingStatus** element specifies the status of the calendar item exception. The **MeetingStatus** element is not supported by certain protocol versions as a child of the **Exception** element. See the details about protocol versions at the end of this section. If the **MeetingStatus** element is not specified as a child element of an **Exception** element, the value of the **MeetingStatus** element for the exception is assumed to be the same as the value of the top-level **MeetingStatus** element.

The **MeetingStatus** element is defined as an element in the **Calendar** namespace. The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The value of the **MeetingStatus** element MUST be one of the values listed in the following table.

  ----------------------------------------------------------------------------------------------------------------------------
  Value   Meaning
  ------- --------------------------------------------------------------------------------------------------------------------
  0       The event is an appointment, which has no attendees.

  1       The event is a [**meeting**](#gt_cbc56efc-e4f7-4b31-9e5f-9c44e3924d94) and the user is the meeting organizer.

  3       This event is a meeting, and the user is not the meeting organizer; the meeting was received from someone else.

  5       The meeting has been canceled and the user was the meeting organizer.

  7       The meeting has been canceled. The user was not the meeting organizer; the meeting was received from someone else.

  9       Same as 1.

  11      Same as 3.

  13      Same as 5.

  15      Same as 7.
  ----------------------------------------------------------------------------------------------------------------------------

The value of the **MeetingStatus** element is sent as an **unsignedByte** but SHOULD be parsed by the client based on the following bit flags.

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
<td>M</td>
<td>R</td>
<td>C</td>
<td colspan="29">unused (variable)</td>
</tr>
<tr class="even">
<td colspan="32">...</td>
</tr>
</tbody>
</table>

**M - Meeting (1 bit):** If set, the event is a meeting. If not set, the event is an appointment.

**R - Received (1 bit):** If set, the user is not the meeting organizer and the meeting was received from someone else. If not set, the user is the meeting organizer.

**C -- Cancelled (1 bit):** If set, the meeting has been canceled. If not set, the meeting is active.

**unused (variable):** These bits are not used. MUST be zero, and MUST be ignored.

The top-level **MeetingStatus** element can be [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -------------------------------------------------------------------------------------
  Protocol version   Element support, top-level   Element support, child of Exception
  ------------------ ---------------------------- -------------------------------------
  2.5                Yes                          

  12.0               Yes                          Yes

  12.1               Yes                          Yes

  14.0               Yes                          Yes

  14.1               Yes                          Yes

  16.0               Yes                          Yes

  16.1               Yes                          Yes
  -------------------------------------------------------------------------------------

When protocol version 2.5 is used, the **MeetingStatus** element is not supported as a child element of the **Exception** element.

#### MonthOfYear

The **MonthOfYear** element is a child element of the **Recurrence** element (section [2.2.2.37](#Section_dabc38cf7f144f518c88717dace42de5)) that specifies the month of the year for the recurrence. It is defined as an element in the **Calendar** namespace.

A command request or response has a minimum of one **MonthOfYear** child element per **Recurrence** element if the value of the **Type** element (section [2.2.2.45](#Section_d418d78b92114faf809fd4652dfd1f12)) is either 5 or 6.

A command request or response has a maximum of one **MonthOfYear** child element per **Recurrence** element.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The value of the **MonthOfYear** element MUST be between 1 and 12.

The **MonthOfYear** element MUST be included in requests or responses when the **Type** element value is either 5 or 6. The **MonthOfYear** element MUST NOT be included in requests or responses when the **Type** element value is zero (0), 1, 2, or 3.

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

#### Name

The **Name** element is a required child element of the **Attendee** element (section [2.2.2.3](#Section_af751263a16647dcb4e7ba411867bd22)) that specifies the name of an attendee. It is defined as an element in the **Calendar** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

A command request or response has only one **Name** child element per **Attendee** element.

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

#### NativeBodyType

The **airsyncbase:NativeBodyType** element is an optional element that specifies how the body text of the calendar item is stored on the server. It is defined as an element in the **AirSyncBase** namespace and used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

For details about the **airsyncbase:NativeBodyType** element, see [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.32.

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

#### Occurrences

The **Occurrences** element is an optional child element of the **Recurrence** element (section [2.2.2.37](#Section_dabc38cf7f144f518c88717dace42de5)) that specifies the number of occurrences before the series ends. It is defined as an element in the **Calendar** namespace.

A command request or response has a maximum of one **Occurrences** child element per **Recurrence** element.

The **Occurrences** element and the **Until** element (section [2.2.2.47](#Section_6c21532bf3024f989eaf05e1ff723f4a)) are mutually exclusive. It is recommended that only one of these elements be included as a child element of a **Recurrence** element (section 2.2.2.37) in a **Sync** command request.

The value of the **Occurrences** element is an **unsignedShort**, as specified in [\[XMLSCHEMA2/2\]](https://go.microsoft.com/fwlink/?LinkId=90609). The maximum value is 999.

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

#### OnlineMeetingConfLink

The **OnlineMeetingConfLink** element is an optional element that contains a [**GRUU**](#gt_72fbc9c5-8485-465c-8b46-64895c8d5102) for an online meeting. It is defined as an element in the **Calendar** namespace. The GRUU can be used by a [**user agent client (UAC)**](#gt_e5f72a3f-9df4-47e1-b4ee-eda52237bafb) to connect to an online conference.

As an optional child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the **OnlineMeetingConfLink** element specifies the GRUU for the exception. A command response has a maximum of one **OnlineMeetingConfLink** child element per **Exception** element.

A command request MUST NOT contain the **OnlineMeetingConfLink** element.

The value of the **OnlineMeetingConfLink** element is either a GRUU as specified in [\[MS-SIPRE\]](%5bMS-SIPRE%5d.pdf#Section_ab4ab24937964ed18cecf496d81a1a83), or an empty tag when included as a child of the **Exception** element.

The **OnlineMeetingConfLink** element can be ghosted. For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

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

#### OnlineMeetingExternalLink

The **OnlineMeetingExternalLink** element is an optional element that contains a [**URL**](#gt_433a4fb7-ef84-46b0-ab65-905f5e3a80b1) for an online meeting. It is defined as an element in the **Calendar** namespace.

As an optional child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the **OnlineMeetingExternalLink** element specifies the [**GRUU**](#gt_72fbc9c5-8485-465c-8b46-64895c8d5102) for the exception. A command response has a maximum of one **OnlineMeetingExternalLink** child element per **Exception** element.

A command request MUST NOT contain the **OnlineMeetingExternalLink** element.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7, or an empty tag when included as a child of the **Exception** element.

If a value for the **OnlineMeetingExternalLink** element exists, it SHOULD be a valid URL.

The **OnlineMeetingExternalLink** element can be ghosted. For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

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

#### OrganizerEmail

The **OrganizerEmail** element is an optional element that specifies the e-mail address of the user who created the calendar item. It is defined as an element in the **Calendar** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

The value of the **OrganizerEmail** element is a **string** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7) in valid e-mail address format, as specified in \[MS-ASDTYPE\] section 2.7.3.

The **OrganizerEmail** element can be ghosted. For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

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

When protocol version 16.0 or 16.1 is used, the client MUST NOT include the **OrganizerEmail** element in command requests and the server will use the email address of the current user.

#### OrganizerName

The **OrganizerName** element is an optional element that specifies the name of the user who created the calendar item. It is defined as an element in the **Calendar** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

The **OrganizerName** element can be ghosted. For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

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

When protocol version 16.0 or 16.1 is used, the client MUST NOT include the **OrganizerName** element in command requests and the server will use the name of the current user.

#### Recurrence

The **Recurrence** element is an optional **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that specifies the [**recurrence pattern**](#gt_4275047f-9935-46db-b9b8-8ca605d16649) for the calendar item. It is defined as an element in the **Calendar** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

The **Recurrence** element can have the following child elements:

-   **Type** (section [2.2.2.45](#Section_d418d78b92114faf809fd4652dfd1f12)): One instance of this element is required in protocol versions 2.5, 12.0, 12.1, 14.0, and 14.1; optional in protocol versions 16.0 and 16.1.

-   **Occurrences** (section [2.2.2.32](#Section_7657d4cc0bac4c1c9b35786cc2c2a5ce)): This element is optional.

-   **Interval** (section [2.2.2.25](#Section_841fec3360944475904627144a4c56fa)): This element is optional.

-   **WeekOfMonth** (section [2.2.2.48](#Section_2cbe8655957149cfaae78f0a29557b9d)): This element is optional.

-   **DayOfWeek** (section [2.2.2.15](#Section_8c1edd6fb34e47599778fa4b211c9d9b)): This element is optional.

-   **MonthOfYear** (section [2.2.2.29](#Section_692cb79e3c2d4ae9ae286079e9ef951e)): This element is optional.

-   **Until** (section [2.2.2.47](#Section_6c21532bf3024f989eaf05e1ff723f4a)): This element is optional.

-   **DayOfMonth** (section [2.2.2.14](#Section_5f50e49cbe9c4c1794e91766af8fb8d2)): This element is optional.

-   **CalendarType** (section [2.2.2.10](#Section_ca68b4ac49e8404ab291e82eb25ac885)): This element is optional in daily and yearly recurrences.

-   **IsLeapMonth** (section [2.2.2.26](#Section_4642e794d9264cf3883e00bf01918001)): This element is optional.

-   **FirstDayOfWeek** (section [2.2.2.24](#Section_438e160210424d6dbed977e2b0855da9)): This element is optional.

The following limitations apply to the **Recurrence** element:

-   The **Recurrence** element MUST NOT specify multiple occurrences that begin on the same day.

-   The **Recurrence** element MUST NOT specify occurrences that overlap with each other or with any exceptions. An exception that modifies the start date of an instance in the [**recurring series**](#gt_2325d666-e02f-49e4-afa5-3e896d672efe) MUST occur on a date that is sometime after the end of the prior instance and before the start of the next instance in the recurring series. The same is true if the prior or next instance in the recurring series is defined as an exception by using the **Exceptions** element.

For more details about recurrence patterns, see [\[MS-OXOCAL\]](%5bMS-OXOCAL%5d.pdf#Section_09861fdec8e440289346e7c214cfdba1) section 2.2.1.44.

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

In protocol version 16.0 and 16.1: Changing the recurrence pattern of a recurring series will delete any exceptions present on the calendar item; the **Type** element is optional in client requests; the client can change a recurring event to a single event by including an empty **Recurrence** element in its request.

##### Recurrence Patterns

Recurrence patterns for recurring calendar items are represented within the **Recurrence** element (section [2.2.2.37](#Section_dabc38cf7f144f518c88717dace42de5)). The **Type** element (section [2.2.2.45](#Section_d418d78b92114faf809fd4652dfd1f12)), which is a child of the **Recurrence** element, specifies the unit of the occurrence (daily, weekly, monthly, or yearly); additional child elements of the **Recurrence** element, such as **Occurrences** (section [2.2.2.32](#Section_7657d4cc0bac4c1c9b35786cc2c2a5ce)) and **DayOfWeek** (section [2.2.2.15](#Section_8c1edd6fb34e47599778fa4b211c9d9b)), are also used to fully define the recurrence pattern over time.

The following lists specify whether elements are required or optional for each value of the **Type** element. It also describes the relationship between the elements, and their meaning for different values of the **Type** element.

For all values of the **Type** element, the following elements are optional:

-   **Occurrences** (section 2.2.2.32) or **Until** (section [2.2.2.47](#Section_6c21532bf3024f989eaf05e1ff723f4a)). Either the **Occurrences** or **Until** element is required to specify an end date. If neither value is set, the event has no end date.

-   **FirstDayOfWeek** (section [2.2.2.24](#Section_438e160210424d6dbed977e2b0855da9)).

When the **Type** element is set to zero (0), meaning a daily occurrence, the following elements are supported:

-   **Interval** (section [2.2.2.25](#Section_841fec3360944475904627144a4c56fa)). Optional.

-   **DayOfWeek**.[\<3\>](\l) Optional. If the **DayOfWeek** element is not set, the recurrence is a daily occurrence, occurring *n* days apart, where *n* is the value of the **Interval** element. If the **DayOfWeek** element is set, the recurrence is a weekly occurrence, occurring on the day specified by the **DayOfWeek** element, and the value of the **Interval** element indicates the number of weeks between occurrences.

When the **Type** element is set to 1, meaning a weekly occurrence, the following elements are supported:

-   **Interval**. Optional.

-   **DayOfWeek**. Required.

When the **Type** element is set to 2, meaning a monthly occurrence, the following elements are supported:

-   **Interval**. Optional.

-   **DayOfMonth** (section [2.2.2.14](#Section_5f50e49cbe9c4c1794e91766af8fb8d2)). Required.

-   **CalendarType** (section [2.2.2.10](#Section_ca68b4ac49e8404ab291e82eb25ac885)). Optional.

When the **Type** element is set to 3, meaning a monthly occurrence on the *n*th day, the following elements are supported:

-   **Interval**. Optional.

-   **WeekOfMonth** (section [2.2.2.48](#Section_2cbe8655957149cfaae78f0a29557b9d)). Required. If the **DayOfWeek** element is set to 127, the **WeekOfMonth** element indicates the day of the month that the event occurs. If the **DayOfWeek** element is set to 62, to specify weekdays, the **WeekOfMonth** element indicates the *n*th weekday of the month, where *n* is the value of **WeekOfMonth** element. If the **DayOfWeek** element is set to 65, to specify weekends, the **WeekOfMonth** element indicates the *n*th weekend day of the month, where *n* is the value of **WeekOfMonth** element.

-   **DayOfWeek**. Required.

-   **CalendarType**. Optional.

When the **Type** element is set to 5, meaning a yearly occurrence, the following elements are supported:

-   **Interval**. Optional.

-   **DayOfMonth**. Required.

-   **MonthOfYear** (section [2.2.2.29](#Section_692cb79e3c2d4ae9ae286079e9ef951e)). Required.

-   **CalendarType**. Optional.

-   **IsLeapMonth** (section [2.2.2.26](#Section_4642e794d9264cf3883e00bf01918001)). Optional

When the **Type** element is set to 6, meaning a yearly occurrence on the *n*th day, the following elements are supported:

-   **Interval**. Optional.

-   **WeekOfMonth**. Required.

-   **DayOfWeek**. Optional.

-   **MonthOfYear**. Required

-   **CalendarType**. Optional.

-   **IsLeapMonth**. Optional

For examples of common recurrence patterns, see section [4.4](#Section_7e47367e85f44548a649e38f2e0f5d66).

Items that equate to the same sequence on the calendar can be represented by different recurrence patterns, as described in section [4.5](#Section_6cff98ad71174e7dbee249f8a5a5809a).

#### Reminder

As a top-level element of the **Calendar** class, the **Reminder** element is an optional element that specifies the number of minutes before the calendar item\'s start time to display a reminder notice. It is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

As an optional child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the **Reminder** element specifies the number of minutes before a calendar item exception\'s start time to display a reminder notice. A command request or response has a maximum of one **Reminder** child element per **Exception** element. If the **Reminder** element is not specified as a child element of an **Exception** element, the value of the **Reminder** element for the exception is assumed to be the same as the value of the top-level **Reminder** element.

The **Reminder** element is defined as an element in the **Calendar** namespace. The value of this element is an **unsignedInt** data type, as specified in [\[XMLSCHEMA2/2\]](https://go.microsoft.com/fwlink/?LinkId=90609), or an **EmptyTag** data type, which contains no value.

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

When protocol version 2.5, 12.0, 12.1, or 14.0 is used, the value of the **Reminder** element cannot be an **EmptyTag** data type. In protocol version 16.0 and 16.1, the client can send a request with an empty **Reminder** element to create an item without a reminder.

#### ResponseRequested

The **ResponseRequested** element is an optional element that specifies whether a response to the meeting request is required. It is defined as an element in the **Calendar** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

The value of the **ResponseRequested** element is a **boolean** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.1.

The **ResponseRequested** element can be ghosted. For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

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

#### ResponseType

As a top-level element of the **Calendar** class, the **ResponseType** element is an optional element that specifies the type of response made by the user to a meeting request.

As an optional child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the **ResponseType** element specifies the type of response made by the user to a recurring meeting exception. If the **ResponseType** element is not specified as a child element of an **Exception** element, the value of the **ResponseType** element for the exception is assumed to be the same as the value of the top-level **ResponseType** element.

A command request MUST NOT include the **ResponseType** element, either as a top-level element or as a child element of the **Exception** element.

A command response has a maximum of one top-level **ResponseType** element per response, and a maximum of one **ResponseType** child element per **Exception** element.

The **ResponseType** element is defined as an element in the **Calendar** namespace. The value of this element is an **unsignedInt** data type, as specified in [\[XMLSCHEMA2/2\]](https://go.microsoft.com/fwlink/?LinkId=90609).

The value of the **ResponseType** element MUST be one of the values listed in the following table.

  ----------------------------------------------------------------------------------------------------------------------
  Value   Meaning
  ------- --------------------------------------------------------------------------------------------------------------
  0       None. The user\'s response to the meeting has not yet been received.

  1       Organizer. The current user is the organizer of the meeting and, therefore, no reply is required.

  2       [**Tentative**](#gt_78bfb817-fde0-4756-9cae-7c68c5c962f5). The user is unsure whether he or she will attend.

  3       Accepted. The user has accepted the meeting request.

  4       Declined. The user has declined the meeting request.

  5       Not Responded. The user has not yet responded to the meeting request.
  ----------------------------------------------------------------------------------------------------------------------

The top-level **ResponseType** element can be ghosted. For details about the use of ghosted properties, see [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.179.

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

#### Sensitivity

As a top-level element of the **Calendar** class, the **Sensitivity** element is an optional child element that specifies the recommended privacy policy for the calendar item. It is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

As an optional child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the **Sensitivity** element specifies the recommended privacy policy for the calendar item exception. A command request or response has a maximum of one **Sensitivity** child element per **Exception** element. If the **Sensitivity** element is not specified as a child element of an **Exception** element, the **Sensitivity** element for the exception is assumed to have the same value as the value of the top-level **Sensitivity** element.

The **Sensitivity** element is defined as an element in the **Calendar** namespace. The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The value of the **Sensitivity** element MUST be one of the values listed in the following table.

  -----------------------------------------------------------------------
  Value                       Meaning
  --------------------------- -------------------------------------------
  0                           Normal

  1                           Personal

  2                           Private

  3                           Confidential
  -----------------------------------------------------------------------

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

When protocol version 2.5 is used, the **Sensitivity** element is required.

#### StartTime

As a top-level element of the **Calendar** class, the **StartTime** element is an optional element that specifies the start time of the calendar item. The client SHOULD include the **StartTime** element in a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21).

For details about server behavior when a calendar event is received that is missing either the **StartTime** element (section 2.2.2.42), the **EndTime** element, or both, see section [3.2.4.4](#Section_d36fecc392244c65b58db4ddb354e93e).

As an optional child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the **StartTime** element specifies the start time of the calendar item exception. If the **StartTime** element is not specified as a child element of an **Exception** element, the value of the **StartTime** element for the exception is assumed to be the same as the value of the top-level **StartTime** element.

The **StartTime** element is defined as an element in the **Calendar** namespace. The value of this element is a **string** data type, represented as a **Compact** **DateTime** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.2).

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

When protocol version 2.5 is used, the **StartTime** element is required.

In protocol version 16.0 and 16.1, changing the start time of a [**recurring series**](#gt_2325d666-e02f-49e4-afa5-3e896d672efe) will delete any exceptions present on the calendar item.

In protocol version 12.0, 12.1, 14.0, 14.1, 16.0, and 16.1, a **Sync** command response MUST contain one instance of the **StartTime** element if more than just **DtStamp** (section [2.2.2.18](#Section_cc4110e7fbb74f01a0e3a29fb2b6d325)) or **AttendeeType** (section [2.2.2.6](#Section_43fee1fc75ae411db2ea55bc290a1ff3)) has changed.

#### Subject

As a top-level element of the **Calendar** class, the **Subject** element is an optional element that specifies the subject of the calendar item. It is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

As an optional child element of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the **Subject** element specifies the subject of the calendar item exception. If the **Subject** element is not specified as a child element of an **Exception** element, the value of this element is assumed to be the same as the value of the top-level **Subject** element.

The **Subject** element is defined as an element in the **Calendar** namespace. The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

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

#### Timezone

The **Timezone** element is an optional element that specifies the time zone of the calendar item. It is defined as an element in the **Calendar** namespace and is used in ActiveSync command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

The value of the **Timezone** element is a **TimeZone** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.6.

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

When protocol version 2.5 is used, the **Timezone** element is required.

#### Type

The **Type** element is a required child element of the **Recurrence** element (section [2.2.2.37](#Section_dabc38cf7f144f518c88717dace42de5)) in protocol versions 2.5, 12.0, 12.1, 14.0, and 14.1; it is an optional child element of the **Recurrence** element in protocol versions 16.0 and 16.1. It specifies the type of the recurrence. It is defined as an element in the **Calendar** namespace.

A command request or response has only one **Type** child element per **Recurrence** element.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The value of the **Type** element MUST be one of the values listed in the following table.

  -----------------------------------------------------------------------
  Value          Meaning
  -------------- --------------------------------------------------------
  0              Recurs daily.

  1              Recurs weekly.

  2              Recurs monthly.

  3              Recurs monthly on the nth day.

  5              Recurs yearly.

  6              Recurs yearly on the nth day.
  -----------------------------------------------------------------------

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

#### UID

The **UID** element is an optional element that specifies an ID that uniquely identifies a single event or [**recurring series**](#gt_2325d666-e02f-49e4-afa5-3e896d672efe). It is defined as an element in the **Calendar** namespace and is used in command requests and responses as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca).

The **UID** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7. The maximum length of the string is 300 characters.

**Protocol Versions**

The following table specifies the protocol versions that support this element. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -------------------------------------------------------------------------------------
  Protocol version   Element support, top-level   Element support, child of Exception
  ------------------ ---------------------------- -------------------------------------
  2.5                Yes                          Yes

  12.0               Yes                          

  12.1               Yes                          

  14.0               Yes                          

  14.1               Yes                          

  16.0               Yes                          

  16.1               Yes                          
  -------------------------------------------------------------------------------------

When protocol version 2.5 is used, the **UID** element is required. It is supported as a child of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)) only when protocol version 2.5 is used.

When protocol version 2.5, 12.0, 12.1, 14.0, or 14.1 is used, the **UID** element is generated by the client when the calendar item is created and is included in a command request. If the **UID** element is not included in the command request, the server MAY[\<4\>](\l) create a value and include it in the command response.

When protocol version 16.0 or 16.1 is used, the **UID** element MUST NOT be present in a command request. Clients can include the **ClientUid** element (section [2.2.2.13](#Section_d22282da0cc54fbab4b45aef341a6c8e)) in a command request to provide a unique, client-derived identifier for a calendar item. When a calendar item is created, the server will generate a unique identifier for the calendar item and return the identifier in the **UID** element of the **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) for an add operation.

#### Until

The **Until** element is an optional child element of the **Recurrence** element (section [2.2.2.37](#Section_dabc38cf7f144f518c88717dace42de5)) that specifies the start time of the last instance of the [**recurring series**](#gt_2325d666-e02f-49e4-afa5-3e896d672efe). It is defined as an element in the **Calendar** namespace.

A command request or response has a maximum of one **Until** child element per **Recurrence** element.

The **Until** element and the **Occurrences** element (section [2.2.2.32](#Section_7657d4cc0bac4c1c9b35786cc2c2a5ce)) are mutually exclusive. It is recommended that only one of these elements be included as a child element of a **Recurrence** element in a **Sync** command request.

The value of the **Until** element is a **string** data type, represented as a **Compact** **DateTime** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.2).

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

#### WeekOfMonth

The **WeekOfMonth** element is a child element of the **Recurrence** element (section [2.2.2.37](#Section_dabc38cf7f144f518c88717dace42de5)) that specifies either the week of the month or the day of the month for the recurrence, depending on the value of the **Type** element. It is defined as an element in the **Calendar** namespace.

A command request or response has a minimum of one **WeekOfMonth** child element per **Recurrence** element when the value of the **Type** element (section [2.2.2.45](#Section_d418d78b92114faf809fd4652dfd1f12)) is either 3 or 6.

A command request or response has a maximum of one **WeekOfMonth** child element per **Recurrence** element.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The value of the **WeekOfMonth** element MUST be between 1 and 5. The value of 5 specifies the last week of the month.

The **WeekOfMonth** element MUST only be included in requests or responses when the **Type** element (section 2.2.2.45) value is either 3 or 6.

When the **Type** element is set to 3, to indicate monthly on the *n*th of the month, the **WeekOfMonth** element MAY be used to specify day of the month on which the event occurs. For more details, see section [2.2.2.37.1](#Section_6b32175ae89d4a9aa715a2e8bab289b0).

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

### Groups

The following table summarizes the set of common XML schema group definitions defined by this specification. XML schema groups that are specific to a particular operation are described with the operation.

  ----------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Group                                                                                    Description
  ---------------------------------------------------------------------------------------- -----------------------------------------------------------------------------
  **TopLevelSchemaProps** (section [2.2.3.1](#Section_58db613cbc4b49d79e826aab860d9a36))   Identifies the elements that are part of the **TopLevelSchemaProps** group.

  ----------------------------------------------------------------------------------------------------------------------------------------------------------------------

#### TopLevelSchemaProps

The **TopLevelSchemaProps** group identifies the following elements as being part of the **TopLevelSchemaProps** group:

-   **Timezone**, as specified in section [2.2.2.44](#Section_a82384b7908a4a11930b7f091bb466ac)

-   **StartTime**, as specified in section [2.2.2.42](#Section_042bc0907eab40d79ebd77fd7c8f0559)

-   **EndTime**, as specified in section [2.2.2.20](#Section_26046deef2af4a7ca9a647ebd43d6873)

-   **Subject**, as specified in section [2.2.2.43](#Section_04ce2d9e2d224df0aea6fa59597e731b)

-   **Location**, as specified in section [2.2.2.27](#Section_db0c360a786e45bd9f1c2932a3e5df2c)

-   **Reminder**, as specified in section [2.2.2.38](#Section_d9b081e091e14ec3a3174ebd0ccd4489)

-   **AllDayEvent**, as specified in section [2.2.2.1](#Section_deb50939a50a4e3dacd2f2031bc628df)

-   **BusyStatus**, as specified in section [2.2.2.9](#Section_9a37b42c67624d3e9e059c9221a1cd06)

-   **Recurrence**, as specified in section [2.2.2.37](#Section_dabc38cf7f144f518c88717dace42de5)

-   **Sensitivity**, as specified in section [2.2.2.41](#Section_9c2b9eeccb794195876dd933c9043cbf)

-   **DtStamp**, as specified in section [2.2.2.18](#Section_cc4110e7fbb74f01a0e3a29fb2b6d325)

-   **Attendees**, as specified in section [2.2.2.4](#Section_8a8db399d4bc4742add06c76d4ada045)

-   **Categories**, as specified in section [2.2.2.11](#Section_4f34a8c7fc8d447a9f7cbcca0665814e)

-   **MeetingStatus**, as specified in section [2.2.2.28](#Section_c040515815e44f28a4ffe296644fef9f)

-   **OrganizerName**, as specified in section [2.2.2.36](#Section_17689af0a79f4ed4bffaf4c1fca731f3)

-   **OrganizerEmail**, as specified in section [2.2.2.35](#Section_873950c9285c470e9e731a67cdc71a9a)

-   **UID**, as specified in section [2.2.2.46](#Section_8f1fb00ca15649e89f54478ecb0ce743)

-   **DisallowNewTimeProposal**, as specified in section [2.2.2.17](#Section_f28bc518cfef4c9c9a2780777b446854)

-   **ResponseRequested**, as specified in section [2.2.2.39](#Section_9e43056042934d9e838c628137993f43)

-   **Exceptions**, as specified in section [2.2.2.22](#Section_2fa6598590d44da79ddf7966531927d7)

The **TopLevelSchemaProps** group is used by the **ItemOperations** command request specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10.

**Protocol Versions**

The following table specifies the protocol versions that support this group. The client indicates the protocol version being used by setting either the MS-ASProtocolVersion header, as specified in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.2.6, or the **Protocol version** field, as specified in \[MS-ASHTTP\] section 2.2.1.1.1.1, in the request.

  -----------------------------------------------------------------------
  Protocol version                    Group support
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

This section describes a conceptual model of possible data organization that an implementation maintains to participate in this protocol. The described organization is provided to facilitate the explanation of how the protocol behaves. This document does not mandate that implementations adhere to this model as long as their external behavior is consistent with that described in this document.

**Calendar class:** A structured [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) text block that adheres to the [**XML schema**](#gt_bd0ce6f9-c350-4900-827e-951265294067) definition specified in section [2.2](#Section_e358c753e61049a3bb671f0679909dcd). It is returned by the server to the client as part of a full XML response to the client command requests that are specified in section [3.1.5](#Section_4710ff8384bc412c85a7ece90e4260c7). **Calendar** class data is included in command requests sent to the server when calendar items need to be retrieved, searched, or synchronized.

**Command request:** A [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc)-formatted message that adheres to the command schemas specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

### Timers

None.

### Initialization

None.

### Higher-Layer Triggered Events

#### Synchronizing Calendar Data Between Client and Server

A client initiates synchronization of **Calendar** class data with the server by sending a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) to the server.

#### Searching a Server for Calendar Data

A client searches for **Calendar** class data on the server by sending a **Search** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.16) to the server.

#### Requesting Details for One or More Calendar Items

A client requests **Calendar** class data for one or more individual calendar items by sending an **ItemOperations** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10) to the server that contains one or more **itemoperations:Fetch** elements (\[MS-ASCMD\] section 2.2.3.67.1).

#### Creating a New Meeting Request

When a user creates a meeting on the client, the client creates a calendar item representing the meeting. In protocol versions 2.5, 12.0, 12.1, 14.0, and 14.1, the client then sends an email with the properly formatted meeting requests to the specified attendees. In protocol version 16.0 and 16.1, the server will send any needed emails when the calendar item is added. As the server receives the attendee responses, the organizer receives updates to the meeting request within the **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21). \[MS-ASCMD\] section 4.16.3 specifies an example that demonstrates a meeting request included in a **Sync** command response.

### Message Processing Events and Sequencing Rules

The following sections specify how elements of the **Calendar** class are used in the context of specific ActiveSync commands. Command details are specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

#### ItemOperations Command Request

A client uses an **ItemOperations** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10) that contains one or more **itemoperations:Fetch** elements (\[MS-ASCMD\] section 2.2.3.67.1) to retrieve data from the server for one or more specific calendar items.

Any of the elements that belong to the **Calendar** class, as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca), can be included in an **ItemOperations** command request.

Top-level **Calendar** class elements, as specified in section 2.2.2, MUST be transmitted as child elements of the **itemoperations:Schema** element (\[MS-ASCMD\] section 2.2.3.158).

For more details about the **ItemOperations** command, see \[MS-ASCMD\] section 2.2.1.10.

#### Search Command Request

A client uses the **Search** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.16) to retrieve **Calendar** class items from the server that match the criteria specified by the client.

Elements that belong to the **Calendar** class, as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca), MUST NOT be included in a **Search** command request.

For more details about the **Search** command, see \[MS-ASCMD\] section 2.2.1.16.

#### Sync Command Request

A client uses the **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) to synchronize its **Calendar** class items for a specified user with the calendar items that are currently stored by the server.

Any of the elements that belong to the **Calendar** class, as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca), can be included in a **Sync** command request.

Top-level **Calendar** class elements, as specified in section 2.2.2, can be transmitted as child elements of the **airsync:ApplicationData** element (\[MS-ASCMD\] section 2.2.3.11) within either an **airsync:Add** element (\[MS-ASCMD\] section 2.2.3.7.2) or an **airsync:Change** element (\[MS-ASCMD\] section 2.2.3.24).

In protocol versions 2.5, 12.0, 12.1, 14.0, and 14.1, top-level **Calendar** class elements can be transmitted as child elements of the **airsync:Supported** element (\[MS-ASCMD\] section 2.2.3.179) in order to support [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5) elements. A specific subset of the **Calendar** class elements is required in this instance. The full list is specified in \[MS-ASCMD\] section 2.2.3.179. In protocol version 16.0 and 16.1, all top-level **Calendar** class elements are ghosted when they are not present.

For more details about the **Sync** command, see \[MS-ASCMD\] section 2.2.1.21.

##### Indicating Deleted Elements in Exceptions

If an element in a recurring calendar item has been deleted in an **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the client sends an empty tag for this element to remove the inherited value from the server.[\<5\>](\l) For example, if the **Location** element (section [2.2.2.27](#Section_db0c360a786e45bd9f1c2932a3e5df2c)) has been deleted for an exception, the client sends an empty **Location** element in a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21).

A client cannot remove an inherited element value from an exception if that property is [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5).

##### Omitting Ghosted Properties from a Sync Change Request

In protocol versions 2.5, 12.0, 12.1, 14.0, and 14.1, when the client sends a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) to the server that contains a nonzero **airsync:SyncKey** element (\[MS-ASCMD\] section 2.2.3.181.4) value, the client uses the **airsync:Supported** element (\[MS-ASCMD\] section 2.2.3.179) within the **Sync** command request to specify which properties are not [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). In subsequent **Sync** command requests, the client includes only the set of **airsync:Supported** elements in the **Sync** command request\'s **airsync:Change** element (\[MS-ASCMD\] section 2.2.3.24). In protocol version 16.0 and 16.1, **Calendar** class elements are ghosted by default and clients SHOULD NOT send unchanged elements in **Sync** command requests.

For more details about ghosted properties, see \[MS-ASCMD\] section 2.2.3.179.

### Timer Events

None.

### Other Local Events

None.

## Server Details

### Abstract Data Model

This section describes a conceptual model of possible data organization that an implementation maintains to participate in this protocol. The described organization is provided to facilitate the explanation of how the protocol behaves. This document does not mandate that implementations adhere to this model as long as their external behavior is consistent with that described in this document.

**Calendar class:** a structured [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) text block that adheres to the [**XML schema**](#gt_bd0ce6f9-c350-4900-827e-951265294067) definition specified in section [2.2](#Section_e358c753e61049a3bb671f0679909dcd). It is returned by the server to the client as part of a full XML response to the client command requests that are specified in section [3.1.5](#Section_4710ff8384bc412c85a7ece90e4260c7). **Calendar** class data is included in command requests sent to the server when calendar items need to be retrieved, searched, or synchronized.

**Command response:** A [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc)-formatted message that adheres to the command schemas specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

### Timers

None.

### Initialization

None.

### Higher-Layer Triggered Events

#### Synchronizing Calendar Data Between Client and Server

Synchronization of **Calendar** class data between client and server is initiated by the client, as specified in section [3.1.4.1](#Section_7e9b1017e41e43fe964a8fcb686cff13). The server responds with a **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21).

#### Searching for Calendar Data

Searching for **Calendar** class data is initiated by the client, as specified in section [3.1.4.2](#Section_a78c11bf941544e5b181d30597915fc7). The server responds with a **Search** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.16).

#### Retrieving Details for One or More Calendar Items

Retrieval of **Calendar** class data for one or more individual calendar items is initiated by the client, as specified in section [3.1.4.3](#Section_b4ff63de6e59478ca92bac4ff35e23f5). The server responds with an **ItemOperations** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10).

#### Creating Calendar Events when the StartTime Element or EndTime Element is Absent

If the server receives a Sync command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) to add a calendar event that is missing either the **StartTime** element (section [2.2.2.42](#Section_042bc0907eab40d79ebd77fd7c8f0559)), the **EndTime** element (section [2.2.2.20](#Section_26046deef2af4a7ca9a647ebd43d6873)), or both, the server attempts to substitute values based on the current time, rounded to the nearest half hour, for the missing values. For example, if the server receives a Sync command request at 18:03 to add a new calendar event, the server rounds the current time to 18:30. The server sets the **StartTime** and **EndTime** elements as specified in the following table.

  -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  StartTime       EndTime         Result
  --------------- --------------- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Absent          Absent          The server sets the value of the **StartTime** element to the rounded current time, and sets the value of the **EndTime** element to the rounded current time plus 30 minutes.

  Absent          In the past     The server includes a **Status** element with a value of 6 in the response, as specified in \[MS-ASCMD\] section 2.2.3.177.17, indicating an error occurred.

  Absent          In the future   The server sets the value of the **StartTime** element to the rounded current time and sets the value of the **EndTime** element to the value of the **EndTime** element in the request. If the rounded current time is after the end time, the server includes a Status element with a value of 6 in the response, indicating an error occurred.

  In the past     Absent          The server sets the value of the **StartTime** element to the value of the **StartTime** element in the request and sets the value of the **EndTime** element to the rounded current time plus 30 minutes.

  In the future   Absent          The server includes a **Status** element with a value of 6 in the response, indicating an error occurred.
  -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

### Message Processing Events and Sequencing Rules

The following information pertains to all command responses:

-   A server MUST recognize when the value of the **Email** element is not formatted as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.3, and MAY replace it with suitable placeholder text.

-   If no action has been taken on a meeting request, the server MUST NOT include the **AppointmentReplyTime** element as a top-level element in a command response. If a meeting request exception has been neither accepted nor tentatively accepted, the server MUST NOT include the **AppointmentReplyTime** element as a child element of the **Exception** element in a command response.

-   The server MUST return a **FirstDayOfWeek** element when the value of the **Type** element (section [2.2.2.45](#Section_d418d78b92114faf809fd4652dfd1f12)) is 1.

-   If the **FirstDayOfWeek** element is not included in the client request, the server SHOULD identify the first day of the week for any recurrence according to the preconfigured options of the user creating the calendar item.

The following sections specify how elements of the **Calendar** class are used in the context of specific ActiveSync commands. Command details are specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

#### ItemOperations Command Response

When a client uses an **ItemOperations** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10), as specified in section [3.1.5.1](#Section_7dbb7edf626a4b0ea944fcf2ebc37af4), to retrieve data from the server for one or more specific calendar items, the server responds with an **ItemOperations** command response (\[MS-ASCMD\] section 2.2.1.10).

Any of the elements that belong to the **Calendar** class, as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca), can be included in an **ItemOperations** command response. If an **airsync:Schema** element (\[MS-ASCMD\] section 2.2.3.158) is included in the **ItemOperations** command request, the elements returned in the **ItemOperations** command response MUST be restricted to the elements that were included as child elements of the **airsync:Schema** element in the command request.

Top-level **Calendar** class elements, as specified in section 2.2.2, MUST be returned as child elements of the **itemoperations:Properties** element (\[MS-ASCMD\] section 2.2.3.139) in the **ItemOperations** command response.

For more details about the **ItemOperations** command, see \[MS-ASCMD\] section 2.2.1.10.

#### Search Command Response

When a client uses the **Search** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.16), as specified in section [3.1.5.2](#Section_cc393fd783cb4f1db5e6ca892beac368), to retrieve **Calendar** class items from the server that match the criteria specified by the client, the server responds with a **Search** command response (\[MS-ASCMD\] section 2.2.1.16).

Any of the elements that belong to the **Calendar** class, as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca), can be included in a **Search** command response.

Top-level **Calendar** class elements MUST be returned as child elements of the **search:Properties** element (\[MS-ASCMD\] section 2.2.3.139) in the **Search** command response.

For more details about the **Search** command, see \[MS-ASCMD\] section 2.2.1.16.

#### Sync Command Response

When a client uses the **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21), as specified in section [3.1.5.3](#Section_7f6102708b9847ee851983f1b5cb9005), to synchronize its **Calendar** class items for a specified user with the calendar items that are currently stored by the server, the server responds with a **Sync** command response (\[MS-ASCMD\] section 2.2.1.21).

Top-level **Calendar** class elements, as specified in section [2.2.2](#Section_0e7d82bcd32f434cafcb3e2f85c01aca), can be included in a **Sync** command response as child elements of the **airsync:ApplicationData** element (\[MS-ASCMD\] section 2.2.3.11) within either an **airsync:Add** element (\[MS-ASCMD\] section 2.2.3.7.2) or an **airsync:Change** element (\[MS-ASCMD\] section 2.2.3.24) in the **Sync** command response.

If one or more properties of an exception for recurring calendar item (that is, any child elements of the **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436))) have been deleted, the server MUST transmit an empty element in the **Sync** command response to indicate that this property is not inherited from the recurrence.

If both the **Occurrences** element (section [2.2.2.32](#Section_7657d4cc0bac4c1c9b35786cc2c2a5ce)) and the **Until** element (section [2.2.2.47](#Section_6c21532bf3024f989eaf05e1ff723f4a)) are included in a **Sync** command request, then the server MUST respect the value of the Occurrences element and ignore the value of the Until element.

The **Sync** command response contains an **airsync:Status** element (\[MS-ASCMD\] section 2.2.3.177.17) with a value of 6 in the following cases:

-   A command request has more than one **CalendarType** element (section [2.2.2.10](#Section_ca68b4ac49e8404ab291e82eb25ac885)) per **Recurrence** element (section [2.2.2.37](#Section_dabc38cf7f144f518c88717dace42de5)) when the **Type** element (section [2.2.2.45](#Section_d418d78b92114faf809fd4652dfd1f12)) value is 2, 3, 5, or 6.

-   The **CalendarType** element is set to one of the following values in the request: 13, 16, 17, 18, 19, 21, 22, or 23.

-   The value of the **FirstDayOfWeek** element (section [2.2.2.24](#Section_438e160210424d6dbed977e2b0855da9)) is outside the range 0 (zero) through 6 (inclusive).

-   The **EndTime** element (section [2.2.2.20](#Section_26046deef2af4a7ca9a647ebd43d6873)) is included in a request and the **StartTime** element is not included in the request.

-   The **DayOfMonth** element (section [2.2.2.14](#Section_5f50e49cbe9c4c1794e91766af8fb8d2)) is included in a request when the value of the **Type** element is not 2 or 5.

-   The **DayOfWeek** element (section [2.2.2.15](#Section_8c1edd6fb34e47599778fa4b211c9d9b)) is included in a request when the value of the **Type** element is not 0 (zero), 1, 3, or 6.

-   The **MonthOfYear** element (section [2.2.2.29](#Section_692cb79e3c2d4ae9ae286079e9ef951e)) is included in a request when the value of the **Type** element is not 5 or 6.

-   The **WeekOfMonth** element (section [2.2.2.48](#Section_2cbe8655957149cfaae78f0a29557b9d)) is included in a request when the value of the **Type** element is not 3 or 6.

For more details about the **Sync** command, see \[MS-ASCMD\] section 2.2.1.21.

##### Removing Exceptions

If an **Exceptions** element (section [2.2.2.22](#Section_2fa6598590d44da79ddf7966531927d7)) is not specified in a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21), then any exceptions previously defined are unchanged, even if the client included the **Exceptions** element as a child of the **Supported** element, as specified in \[MS-ASCMD\] section 2.2.3.179. If a particular **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)) is excluded in a **Sync** command request, then that particular exception remains unchanged.

##### Indicating Deleted Elements in Exceptions

If an element of a recurring calendar item has been deleted in an **Exception** element (section [2.2.2.21](#Section_b2d14c0c8d254049818b0e9025d32436)), the server MUST send an empty tag for this element in the **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21). For example, if the **Location** element (section [2.2.2.27](#Section_db0c360a786e45bd9f1c2932a3e5df2c)) has been deleted for an exception, the server sends an empty **Location** element in the **Sync** command response.

##### Omitting Ghosted Properties from a Sync Change Request

When the client sends a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) to the server that contains a nonzero **airsync:SyncKey** element (\[MS-ASCMD\] section 2.2.3.181.4) value, the client uses the **airsync:Supported** element within the **Sync** command request to specify which properties are not [**ghosted**](#gt_c2354a51-451b-4296-88cd-3321c437d2c5). In subsequent **Sync** command requests, the client includes only these elements in the **Sync** command request\'s **airsync:Change** element (\[MS-ASCMD\] section 2.2.3.24). Ghosted elements are not sent to the server. Instead of deleting these excluded properties, the server preserves their previous value.

For more details about ghosted properties, see \[MS-ASCMD\] section 2.2.3.179.

### Timer Events

None.

### Other Local Events

None.

# Protocol Examples

## Synchronizing Calendar Data

The following example demonstrates a client request to synchronize calendar data with the server, and the server response. Elements of the **Calendar** class are child elements of the **airsync:ApplicationData** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.11) under the **airsync:Add** element (\[MS-ASCMD\] section 2.2.3.7.2) and the **airsync:Change** element (\[MS-ASCMD\] section 2.2.3.24) in the server response.

Request:

1.  \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

    \<Sync xmlns=\"AirSync:\"\>

    \<Collections\>

    \<Collection\>

    \<SyncKey\>850479756\</SyncKey\>

    \<CollectionId\>1\</CollectionId\>

    \<DeletesAsMoves/\>

    \<GetChanges/\>

    \</Collection\>

    \</Collections\>

    \</Sync\>

Response:

12. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

    \<Sync xmlns=\"AirSync:\" xmlns:calendar=\"Calendar:\" xmlns:airsyncbase=\"AirSyncBase:\"\>

    \<Collections\>

    \<Collection\>

    \<SyncKey\>664578668\</SyncKey\>

    \<CollectionId\>1\</CollectionId\>

    \<Status\>1\</Status\>

    \<Commands\>

    \<Change\>

    \<ServerId\>1:12\</ServerId\>

    \<ApplicationData\>

    \<calendar:Timezone\>4AEAAFAAYQBjAGkAZgBpAGMAIABTAHQAYQBuAGQAYQByAGQAIABUAGkAbQBlAAAAA

    AAAAAAAAAAAAAAAAAAAAAAAAAAAAAsAAAABAAIAAAAAAAAAAAAAAFAAYQBjAGkAZgBpAGMAIABEAG

    EAeQBsAGkAZwBoAHQAIABUAGkAbQBlAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMAAAACAAIAAAA

    AAAAAxP///w==\</calendar:Timezone\>

    \<calendar:DtStamp\>20081002T231357Z\</calendar:DtStamp\>

    \<calendar:StartTime\>20081010T190000Z\</calendar:StartTime\>

    \<calendar:Subject\>Lunch meeting\</calendar:Subject\>

    \<calendar:UID\>040000008200E00074C5B7101A82E008000000001027EAEDA124C901000000000000000010000000C58EA426C0CFF24AB3125200707153B1\</calendar:UID\>

    \<calendar:OrganizerName\>Anat Kerry\</calendar:OrganizerName\>

    \<calendar:OrganizerEmail\>anat@contoso.com\</calendar:OrganizerEmail\>

    \<calendar:Location\>Cafeteria A\</calendar:Location\>

    \<calendar:EndTime\>20081010T203000Z\</calendar:EndTime\>

    \<airsyncbase:Body\>

    \<airsyncbase:Type\>3\</airsyncbase:Type\>

    \<airsyncbase:EstimatedDataSize\>5669\</airsyncbase:EstimatedDataSize\>

    \<airsyncbase:Truncated\>1\</airsyncbase:Truncated\>

    \</airsyncbase:Body\>

    \<calendar:Sensitivity\>0\</calendar:Sensitivity\>

    \<calendar:BusyStatus\>3\</calendar:BusyStatus\>

    \<calendar:AllDayEvent\>0\</calendar:AllDayEvent\>

    \<calendar:Reminder\>15\</calendar:Reminder\>

    \<calendar:MeetingStatus\>0\</calendar:MeetingStatus\>

    \<airsyncbase:NativeBodyType\>3\</airsyncbase:NativeBodyType\>

    \</ApplicationData\>

    \</Change\>

    \<Add\>

    \<ServerId\>1:13\</ServerId\>

    \<ApplicationData\>

    \<calendar:Timezone\>4AEAAFAAYQBjAGkAZgBpAGMAIABTAHQAYQBuAGQAYQByAGQAIABUAGkAbQBlAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAsAAAABAAIAAAAAAAAAAAAAAFAAYQBjAGkAZgBpAGMAIABEAGEAeQBsAGkAZwBoAHQAIABUAGkAbQBlAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMAAAACAAIAAAAAAAAAxP///w==\</calendar:Timezone\>

    \<calendar:DtStamp\>20081002T231335Z\</calendar:DtStamp\>

    \<calendar:StartTime\>20081013T170000Z\</calendar:StartTime\>

    \<calendar:Subject\>Dry Run of TechEd Presentation\</calendar:Subject\>

    \<calendar:UID\>040000008200E00074C5B7101A82E008000000009003C9E1A924C901000000000000000010000000B3635D1E1A2FF54FA575AB96797F532F\</calendar:UID\>

    \<calendar:OrganizerName\>Anat Kerry\</calendar:OrganizerName\>

    \<calendar:OrganizerEmail\>anatcontoso.com\</calendar:OrganizerEmail\>

    \<calendar:Location\>Conf Room 33-A/1298\</calendar:Location\>

    \<calendar:EndTime\>20081013T180000Z\</calendar:EndTime\>

    \<airsyncbase:Body\>

    \<airsyncbase:Type\>3\</airsyncbase:Type\>

    \<airsyncbase:EstimatedDataSize\>5669\</airsyncbase:EstimatedDataSize\>

    \<airsyncbase:Truncated\>1\</airsyncbase:Truncated\>

    \</airsyncbase:Body\>

    \<calendar:Sensitivity\>0\</calendar:Sensitivity\>

    \<calendar:BusyStatus\>2\</calendar:BusyStatus\>

    \<calendar:AllDayEvent\>0\</calendar:AllDayEvent\>

    \<calendar:Reminder\>15\</calendar:Reminder\>

    \<calendar:MeetingStatus\>0\</calendar:MeetingStatus\>

    \<airsyncbase:NativeBodyType\>3\</airsyncbase:NativeBodyType\>

    \</ApplicationData\>

    \</Add\>

    \<Add\>

    \<ServerId\>1:14\</ServerId\>

    \<ApplicationData\>

    \<calendar:Timezone\>4AEAAFAAYQBjAGkAZgBpAGMAIABTAHQAYQBuAGQAYQByAGQAIABUAGkAbQBlAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAsAAAABAAIAAAAAAAAAAAAAAFAAYQBjAGkAZgBpAGMAIABEAGEAeQBsAGkAZwBoAHQAIABUAGkAbQBlAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMAAAACAAIAAAAAAAAAxP///w==\</calendar:Timezone\>

    \<calendar:DtStamp\>20081002T231639Z\</calendar:DtStamp\>

    \<calendar:StartTime\>20081013T190000Z\</calendar:StartTime\>

    \<calendar:Subject\>Team Meeting\</calendar:Subject\>

    \<calendar:UID\>040000008200E00074C5B7101A82E0080000000060043DFCA924C90100000000000000001000000097F14EF755AC454BA30EFA7B1B315E43\</calendar:UID\>

    \<calendar:OrganizerName\>Anat Kerry\</calendar:OrganizerName\>

    \<calendar:OrganizerEmail\>anat@contoso.com\</calendar:OrganizerEmail\>

    \<calendar:Location\>My office\</calendar:Location\>

    \<calendar:EndTime\>20081013T193000Z\</calendar:EndTime\>

    \<calendar:Recurrence\>

    \<calendar:Type\>3\</calendar:Type\>

    \<calendar:Interval\>1\</calendar:Interval\>

    \<calendar:Until\>20090713T190000Z\</calendar:Until\>

    \<calendar:WeekOfMonth\>2\</calendar:WeekOfMonth\>

    \<calendar:DayOfWeek\>2\</calendar:DayOfWeek\>

    \</calendar:Recurrence\>

    \<airsyncbase:Body\>

    \<airsyncbase:Type\>3\</airsyncbase:Type\>

    \<airsyncbase:EstimatedDataSize\>5769\</airsyncbase:EstimatedDataSize\>

    \<airsyncbase:Truncated\>1\</airsyncbase:Truncated\>

    \</airsyncbase:Body\>

    \<calendar:Sensitivity\>0\</calendar:Sensitivity\>

    \<calendar:BusyStatus\>2\</calendar:BusyStatus\>

    \<calendar:AllDayEvent\>0\</calendar:AllDayEvent\>

    \<calendar:Reminder\>15\</calendar:Reminder\>

    \<calendar:MeetingStatus\>0\</calendar:MeetingStatus\>

    \<airsyncbase:NativeBodyType\>3\</airsyncbase:NativeBodyType\>

    \</ApplicationData\>

    \</Add\>

    \</Commands\>

    \</Collection\>

    \</Collections\>

    \</Sync\>

## Synchronizing Recurring Appointments with Exceptions

The following example demonstrates a client request to synchronize calendar data with the server, and the server response. In this example, the server response contains a weekly recurring appointment with a single exception.

Request:

109. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<Sync xmlns:calendar=\"Calendar:\" xmlns:airsyncbase=\"AirSyncBase:\" xmlns=\"AirSync:\"\>

     \<Collections\>

     \<Collection\>

     \<SyncKey\>1958804782\</SyncKey\>

     \<CollectionId\>1\</CollectionId\>

     \<DeletesAsMoves\>1\</DeletesAsMoves\>

     \<GetChanges\>1\</GetChanges\>

     \<WindowSize\>512\</WindowSize\>

     \</Collection\>

     \</Collections\>

     \</Sync\>

Response:

121. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<Sync xmlns:calendar=\"Calendar:\" xmlns:airsyncbase=\"AirSyncBase:\" xmlns=\"AirSync:\"\>

     \<Collections\>

     \<Collection\>

     \<SyncKey\>573512161\</SyncKey\>

     \<CollectionId\>1\</CollectionId\>

     \<Status\>1\</Status\>

     \<Commands\>

     \<Add\>

     \<ServerId\>1:1\</ServerId\>

     \<ApplicationData\>

     \<calendar:Timezone\>4AEAACgARwBNAFQALQAwADgAOgAwADAAKQAgAFAAYQBjAGkAZgBpAGMAIABUAGkA

     bQBlACAAKABVAFMAIAAmACAAQwAAAAsAAAABAAIAAAAAAAAAAAAAACgARwBNAFQALQAwADgAOgAwA

     DAAKQAgAFAAYQBjAGkAZgBpAGMAIABUAGkAbQBlACAAKABVAFMAIAAmACAAQwAAAAMAAAACAAIAAA

     AAAAAAxP///w==\</calendar:Timezone\>

     \<calendar:DtStamp\>20090415T165811Z\</calendar:DtStamp\>

     \<calendar:StartTime\>20090417T170000Z\</calendar:StartTime\>

     \<calendar:Subject\>Recurring appointment test\</calendar:Subject\>

     \<calendar:UID\>040000008200E00074C5B7101A82E00800000000B0CD1F52EBBDC9010000000000000

     00010000000B05E442FCB2CA443BF3D99B51A729FE6\</calendar:UID\>

     \<calendar:OrganizerName\>Anat Kerry\</calendar:OrganizerName\>

     \<calendar:OrganizerEmail\>anat@contoso.com \</calendar:OrganizerEmail\>

     \<calendar:Location\>My office\</calendar:Location\>

     \<calendar:EndTime\>20090417T180000Z\</calendar:EndTime\>

     \<calendar:Recurrence\>

     \<calendar:Type\>1\</calendar:Type\>

     \<calendar:Interval\>1\</calendar:Interval\>

     \<calendar:Occurrences\>3\</calendar:Occurrences\>

     \<calendar:DayOfWeek\>32\</calendar:DayOfWeek\>

     \</calendar:Recurrence\>

     \<airsyncbase:Body\>

     \<airsyncbase:Type\>3\</airsyncbase:Type\>

     \<airsyncbase:EstimatedDataSize\>238\</airsyncbase:EstimatedDataSize\>

     \<airsyncbase:Truncated\>1\</airsyncbase:Truncated\>

     \</airsyncbase:Body\>

     \<calendar:Sensitivity\>0\</calendar:Sensitivity\>

     \<calendar:BusyStatus\>2\</calendar:BusyStatus\>

     \<calendar:AllDayEvent\>0\</calendar:AllDayEvent\>

     \<calendar:Reminder\>15\</calendar:Reminder\>

     \<calendar:Exceptions\>

     \<calendar:Exception\>

     \<calendar:Deleted\>1\</calendar:Deleted\>

     \<calendar:ExceptionStartTime\>20090424T170000Z\</calendar:ExceptionStartTime\>

     \</calendar:Exception\>

     \</calendar:Exceptions\>

     \<calendar:MeetingStatus\>0\</calendar:MeetingStatus\>

     \<airsyncbase:NativeBodyType\>3\</airsyncbase:NativeBodyType\>

     \<calendar:ResponseRequested\>1\</calendar:ResponseRequested\>

     \<calendar:ResponseType\>1\</calendar:ResponseType\>

     \</ApplicationData\>

     \</Add\>

     \</Commands\>

     \</Collection\>

     \</Collections\>

     \</Sync\>

## Setting Attendee Status from the Server

The following example demonstrates a **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) from the server that contains a new meeting, and a **Sync** command response from the server that shows changes to the calendar item that reflect an attendee has accepted the meeting invitation.

In the following **Sync** command response, the new meeting has one attendee. The organizer is not included in the attendee list; rather, the organizer\'s information is specified by the **calendar:OrganizerEmail** element (section [2.2.2.35](#Section_873950c9285c470e9e731a67cdc71a9a)) and the **calendar:OrganizerName** (section [2.2.2.36](#Section_17689af0a79f4ed4bffaf4c1fca731f3)) element.

176. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<Sync xmlns=\"AirSync:\" xmlns:calendar=\"Calendar:\"

     xmlns:airsyncbase=\"AirSyncBase:\"\>

     \<Collections\>

     \<Collection\>

     \<Class\>Calendar\</Class\>

     \<SyncKey\>3\</SyncKey\>

     \<CollectionId\>1\</CollectionId\>

     \<Status\>1\</Status\>

     \<Commands\>

     \<Add\>

     \<ServerId\>1:2\</ServerId\>

     \<ApplicationData\>

     \<calendar:Timezone\>4AEAAFAAYQBjAGkAZgBpAGMAIAB

     TAHQAYQBuAGQAYQByAGQAIABUAGkAbQBlAAAAAAAAAAA

     AAAAAAAAAAAAAAAAAAAAAAAoAAAAFAAIAAAAAAAAAAA

     AAAFAAYQBjAGkAZgBpAGMAIABEAGEAeQBsAGkAZwBoAHQA

     IABUAGkAbQBlAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA

     QAAAABAAIAAAAAAAAAxP///w==\</calendar:Timezone\>

     \<calendar:DtStamp\>20051103T010509Z\</calendar:DtStamp\>

     \<calendar:StartTime\>20051103T230000Z\</calendar:StartTime\>

     \<calendar:Subject\>test meeting\</calendar:Subject\>

     \<calendar:UID\>040000008200E00074C5B7101A82E0080000000

     0B0FD68A212E0C5010000000000000000100000008C46B9A4960AF

     340871367CEC57B4543\</calendar:UID\>

     \<calendar:Attendees\>

     \<calendar:Attendee\>

     \<calendar:Email\>chris@fourthcoffee.com

     \</calendar:Email\>

     \<calendar:Name\>Chris Gray\</calendar:Name\>

     \<calendar:AttendeeStatus\>0\</calendar:AttendeeStatus\>

     \<calendar:AteendeeType\>1\</calendar:AttendeeType\>

     \</calendar:Attendee\>

     \</calendar:Attendees\>

     \<calendar:OrganizerName\>Anat Kerry

     \</calendar:OrganizerName\>

     \<calendar:OrganizerEmail\>anat@contoso.com\</calendar:OrganizerEmail\>

     \<calendar:Location\>34/1123\</calendar:Location\>

     \<calendar:EndTime\>20051104T000000Z\</calendar:EndTime\>

     \<airsyncbase:Body\>

     \<airsyncbase:Type\>1\</airsyncbase:Type\>

     \<airsyncbase:EstimatedDataSize\>28

     \</airsyncbase:EstimatedDataSize\>

     \</airsyncbase:Body\>

     \<calendar:Sensitivity\>0\</calendar:Sensitivity\>

     \<calendar:BusyStatus\>2\</calendar:BusyStatus\>

     \<calendar:AllDayEvent\>0\</calendar:AllDayEvent\>

     \<calendar:Reminder\>15\</calendar:Reminder\>

     \<calendar:MeetingStatus\>1\</calendar:MeetingStatus\>

     \</ApplicationData\>

     \</Add\>

     \</Commands\>

     \</Collection\>

     \</Collections\>

     \</Sync\>

The following **Sync** command response contains a change to the calendar item that reflects that an attendee has accepted the meeting invitation. In this example, the value of the **calendar:AttendeeStatus** element (section [2.2.2.5](#Section_9e773c231f154f8e95e7bc9cc6ec5592)) is 3, indicating that the attendee has accepted the meeting invitation.

231. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<Sync xmlns=\"AirSync:\" xmlns:calendar=\"Calendar:\"

     xmlns:airsyncbase=\"AirSyncBase:\"\>

     \<Collections\>

     \<Collection\>

     \<Class\>Calendar\</Class\>

     \<SyncKey\>4\</SyncKey\>

     \<CollectionId\>1\</CollectionId\>

     \<Status\>1\</Status\>

     \<Commands\>

     \<Change\>

     \<ServerId\>1:2\</ServerId\>

     \<ApplicationData\>

     \<calendar:Timezone\>4AEAAFAAYQBjAGkAZgBpAGMAIABTAHQAY

     QBuAGQAYQByAGQAIABUAGkAbQBlAAAAAAAAAAAAAAAAAAAAAAAAA

     AAAAAAAAAoAAAAFAAIAAAAAAAAAAAAAFAAYQBjAGkAZgBpAGMAIA

     BEAGEAeQBsAGkAZwBoAHQAIABUAGkAbQBlAAAAAAAAAAAAAAAAAAA

     AAAAAAAAAAAAAAAQAAAABAAIAAAAAAAAAxP///w==

     \</calendar:Timezone\>

     \<calendar:DtStamp\>20051103T013759Z\</calendar:DtStamp\>

     \<calendar:StartTime\>20051103T230000Z\</calendar:StartTime\>

     \<calendar:Subject\>test meeting\</calendar:Subject\>

     \<calendar:UID\>040000008200E00074C5B7101A82E00800000000B

     0FD68A212E0C5010000000000000000100000008C46B9A4960AF

     340871367CEC57B4543\</calendar:UID\>

     \<calendar:Attendees\>

     \<calendar:Attendee\>

     \<calendar:Email\>chris@fourthcoffee.com

     \</calendar:Email\>

     \<calendar:Name\>Chris Gray\</calendar:Name\>

     \<calendar:AttendeeStatus\>3\</calendar:AttendeeStatus\>

     \<calendar:AttendeeType\>1\</calendar:AttendeeType\>

     \</calendar:Attendee\>

     \</calendar:Attendees\>

     \<calendar:OrganizerName\>Anat Kerry

     \</calendar:OrganizerName\>

     \<calendar:OrganizerEmail\>anat@contoso.com\</calendar:OrganizerEmail\>

     \<calendar:Location\>34/1123\</calendar:Location\>

     \<calendar:EndTime\>20051104T000000Z\</calendar:EndTime\>

     \<airsyncbase:Body\>

     \<airsyncbase:Type\>1\</airsyncbase:Type\>

     \<airsyncbase:EstimatedDataSize\>28\</airsyncbase:EstimatedDataSize\>

     \</airsyncbase:Body\>

     \<calendar:Sensitivity\>0\</calendar:Sensitivity\>

     \<calendar:BusyStatus\>2\</calendar:BusyStatus\>

     \<calendar:AllDayEvent\>0\</calendar:AllDayEvent\>

     \<calendar:Reminder\>15\</calendar:Reminder\>

     \<calendar:MeetingStatus\>1\</calendar:MeetingStatus\>

     \</ApplicationData\>

     \</Change\>

     \</Commands\>

     \</Collection\>

     \</Collections\>

     \</Sync\>

## Creating Recurring Calendar Items

The following examples demonstrate how to create common daily, monthly, and yearly recurrence patterns.

The following is a daily event, occurring every day.

285. \<Recurrence\>

     \<Type\>0\</Type\>

     \<Interval\>1\</Interval\>

     \</Recurrence\>

The following is a daily event, occurring every other day.

289. \<Recurrence\>

     \<Type\>0\</Type\>

     \<Interval\>2\</Interval\>

     \</Recurrence\>

The following is a weekly event, occurring every weekday.

293. \<Recurrence\>

     \<Type\>1\</Type\>

     \<Interval\>1\</Interval\>

     \<DayOfWeek\>62\</DayOfWeek\>

     \</Recurrence\>

The following is a weekly event, occurring every Saturday.

298. \<Recurrence\>

     \<Type\>1\</Type\>

     \<Interval\>1\</Interval\>

     \<DayOfWeek\>64\</DayOfWeek\>

     \</Recurrence\>

The following is a monthly event, occurring on the first day of every month.

303. \<Recurrence\>

     \<Type\>2\</Type\>

     \<Interval\>1\</Interval\>

     \<DayOfMonth\>1\</DayOfMonth\>

     \</Recurrence\>

The following is a monthly event, occurring on the last day of every month.

308. \<Recurrence\>

     \<Type\>3\</Type\>

     \<Interval\>1\</Interval\>

     \<WeekOfMonth\>5\</WeekOfMonth\>

     \<DayOfWeek\>127\</DayOfWeek\>

     \</Recurrence\>

The following is a monthly event, occurring on the first Saturday of every month.

314. \<Recurrence\>

     \<Type\>3\</Type\>

     \<Interval\>1\</Interval\>

     \<WeekOfMonth\>1\</WeekOfMonth\>

     \<DayOfWeek\>64\</DayOfWeek\>

     \</Recurrence\>

The following is a monthly event, occurring on the first weekday of every month.

320. \<Recurrence\>

     \<Type\>3\</Type\>

     \<Interval\>1\</Interval\>

     \<WeekOfMonth\>1\</WeekOfMonth\>

     \<DayOfWeek\>62\</DayOfWeek\>

     \</Recurrence\>

The following is a monthly event, occurring on the first weekend day of every month.

326. \<Recurrence\>

     \<Type\>3\</Type\>

     \<Interval\>1\</Interval\>

     \<WeekOfMonth\>1\</WeekOfMonth\>

     \<DayOfWeek\>65\</DayOfWeek\>

     \</Recurrence\>

The following is a yearly event, occurring on the first day of June, every year.

332. \<Recurrence\>

     \<Type\>5\</Type\>

     \<Interval\>1\</Interval\>

     \<DayOfMonth\>1\</DayOfMonth\>

     \<MonthOfYear\>6\</MonthOfYear\>

     \</Recurrence\>

The following is a yearly event, occurring on the first Saturday of June, every year.

338. \<Recurrence\>

     \<Type\>6\</Type\>

     \<Interval\>1\</Interval\>

     \<WeekOfMonth\>1\</WeekOfMonth\>

     \<DayOfWeek\>64\</DayOfWeek\>

     \<MonthOfYear\>6\</MonthOfYear\>

     \</Recurrence\>

The following is a yearly event, occurring on the last day of June each month.

345. \<Recurrence\>

     \<Type\>6\</Type\>

     \<Interval\>1\</Interval\>

     \<WeekOfMonth\>5\</WeekOfMonth\>

     \<DayOfWeek\>127\</DayOfWeek\>

     \<MonthOfYear\>6\</MonthOfYear\>

     \</Recurrence\>

## Recurrence Patterns that Resolve to the Same Recurring Calendar Item

As specified in section [2.2.2.37.1](#Section_6b32175ae89d4a9aa715a2e8bab289b0), is possible to create the same recurring event using different recurrence patterns. For example, both of the following recurrence patterns create events on the second day of the month.

352. \<Recurrence\>

     //The Type element is set to monthly recurrence (2).

     \<Type\>2\</Type\>

     //The Interval element is set to occur every month (1).

     \<Interval\>1\</Interval\>

     //The DayOfMonth element is set to occur on the second day of the month (2).

     \<DayOfMonth\>2\</DayOfMonth\>

     //The CalendarType element is set to Gregorian (1).

     \<CalendarType\>1\</CalendarType\>

     \</Recurrence\>

     \<Recurrence\>

     //The Type element is set to monthly recurrence on the nth day (3).

     \<Type\>3\</Type\>

     //The Interval element is set to occur every month (1).

     \<Interval\>1\</Interval\>

     //The WeekOfMonth element is set to occur on the second day of the month,

     //because the DayOfWeek element is set to 127.

     \<WeekOfMonth\>2\</WeekOfMonth\>

     \<DayOfWeek\>127\</DayOfWeek\>

     //The CalendarType element is set to Gregorian (1).

     \<CalendarType\>1\</CalendarType\>

     \</Recurrence\>

Both of the following recurrence patterns create events that occur every Saturday.

375. \<Recurrence\>

     //The Type element is set to daily recurrence (0).

     \<Type\>0\</Type\>

     //The Interval element is set to occur every day (1).

     \<Interval\>1\</Interval\>

     //Because the DayOfWeek element is set, the value of the Interval

     //element indicates the number of weeks between each occurrence.

     //The DayOfWeek element is set to Saturday (64).

     \<DayOfWeek\>64\</DayOfWeek\>

     \</Recurrence\>

     \<Recurrence\>

     //The Type element is set to weekly recurrence (0).

     \<Type\>1\</Type\>

     //The Interval element is set to occur every week (1).

     \<Interval\>1\</Interval\>

     //Because the DayOfWeek element is set, the value of the Interval

     //element indicates the number of weeks between each occurrence.

     //The DayOfWeek element is set to Saturday (64).

     \<DayOfWeek\>64\</DayOfWeek\>\
     \</Recurrence\>

# Security

## Security Considerations for Implementers

None.

## Index of Security Parameters

None.

# Appendix A: Full XML Schema

For ease of implementation, this section contains the contents of the Calendar.xsd file, which represents the full XML schema for this protocol. This schema is valid for protocol versions 2.5, 12.0, 12.1, 14.0, 14.1, 16.0 and 16.1. The additional files that this schema file requires to operate correctly are listed in the following table.

  ------------------------------------------------------------------------------------------------------------------------------
  File name                           Defining specification
  ----------------------------------- ------------------------------------------------------------------------------------------
  AirSyncBase.xsd                     [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 6

  MeetingResponseRequest.xsd          [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 6.25
  ------------------------------------------------------------------------------------------------------------------------------

395. \<?xml version=\"1.0\" encoding=\"UTF-8\"?\>

     \<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:airsyncbase=

     \"AirSyncBase\" xmlns=\"Calendar\" targetNamespace=\"Calendar\"

     elementFormDefault=\"qualified\" attributeFormDefault=\"unqualified\"\>

     \<xs:import namespace=\"AirSyncBase\" schemaLocation=\"AirSyncBase.xsd\"/\>

     \<xs:import namespace=\"MeetingResponse\" schemaLocation=\"MeetingResponseRequest.xsd\"/\>

     \<xs:element name=\"Timezone\" type=\"xs:string\"/\>

     \<xs:element name=\"AllDayEvent\" type=\"xs:unsignedByte\"/\>

     \<xs:element name=\"Body\" type=\"xs:string\"/\>

     \<xs:element name=\"BodyTruncated\" type=\"xs:boolean\"/\>

     \<xs:element name=\"BusyStatus\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:unsignedByte\"\>

     \<xs:minInclusive value=\"0\"/\>

     \<xs:maxInclusive value=\"5\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element name=\"OrganizerName\" type=\"xs:string\"/\>

     \<xs:element name=\"OrganizerEmail\" type=\"xs:string\"/\>

     \<xs:element name=\"DtStamp\" type=\"xs:string\"/\>

     \<xs:element name=\"EndTime\" type=\"xs:string\"/\>

     \<xs:element name=\"Location\" type=\"xs:string\"/\>

     \<xs:element name=\"Reminder\" type=\"xs:unsignedInt\"/\>

     \<xs:element name=\"Sensitivity\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:unsignedByte\"\>

     \<xs:minInclusive value=\"0\"/\>

     \<xs:maxInclusive value=\"3\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element name=\"Subject\" type=\"xs:string\"/\>

     \<xs:element name=\"StartTime\" type=\"xs:string\"/\>

     \<xs:element name=\"UID\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:string\"\>

     \<xs:maxLength value=\"300\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element name=\"MeetingStatus\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:unsignedByte\"\>

     \<xs:enumeration value=\"1\"/\>

     \<xs:enumeration value=\"0\"/\>

     \<xs:enumeration value=\"3\"/\>

     \<xs:enumeration value=\"5\"/\>

     \<xs:enumeration value=\"7\"/\>

     \<xs:enumeration value=\"9\"/\>

     \<xs:enumeration value=\"11\"/\>

     \<xs:enumeration value=\"13\"/\>

     \<xs:enumeration value=\"15\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element name=\"Attendees\"\>

     \<xs:complexType\>

     \<xs:sequence minOccurs=\"0\"\>

     \<xs:element name=\"Attendee\" maxOccurs=\"unbounded\"\>

     \<xs:complexType\>

     \<xs:all\>

     \<xs:element name=\"Email\" type=\"xs:string\"/\>

     \<xs:element name=\"Name\" type=\"xs:string\"/\>

     \<xs:element name=\"AttendeeStatus\" minOccurs=\"0\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:unsignedByte\"\>

     \<xs:enumeration value=\"0\"/\>

     \<xs:enumeration value=\"2\"/\>

     \<xs:enumeration value=\"3\"/\>

     \<xs:enumeration value=\"4\"/\>

     \<xs:enumeration value=\"5\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element name=\"AttendeeType\" minOccurs=\"0\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:unsignedByte\"\>

     \<xs:enumeration value=\"1\"/\>

     \<xs:enumeration value=\"2\"/\>

     \<xs:enumeration value=\"3\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element ref=\"MeetingResponse:ProposedStartTime\" minOccurs=\"0\"/\>

     \<xs:element ref=\"MeetingResponse:ProposedEndTime\" minOCcurs=\"0\"/\>

     \</xs:all\>

     \</xs:complexType\>

     \</xs:element\>

     \</xs:sequence\>

     \</xs:complexType\>

     \</xs:element\>

     \<xs:element name=\"Categories\"\>

     \<xs:complexType\>

     \<xs:sequence minOccurs=\"0\"\>

     \<xs:element name=\"Category\" type=\"xs:string\" maxOccurs=\"300\"/\>

     \</xs:sequence\>

     \</xs:complexType\>

     \</xs:element\>

     \<xs:element name=\"ClientUid\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:string\"\>

     \<xs:minLength value=\"1\"/\>

     \<xs:maxLength value=\"300\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element name=\"Recurrence\"\>

     \<xs:complexType\>

     \<xs:all minOccurs=\"0\"\>

     \<xs:element name=\"Type\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:unsignedByte\"\>

     \<xs:minInclusive value=\"0\"/\>

     \<xs:maxInclusive value=\"6\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element name=\"Occurrences\" type=\"xs:unsignedShort\" minOccurs=\"0\"/\>

     \<xs:element name=\"Interval\" minOccurs=\"0\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:unsignedShort\"\>

     \<xs:minInclusive value=\"0\"/\>

     \<xs:maxInclusive value=\"999\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element name=\"WeekOfMonth\" minOccurs=\"0\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:unsignedByte\"\>

     \<xs:minInclusive value=\"1\"/\>

     \<xs:maxInclusive value=\"5\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element name=\"DayOfWeek\" minOccurs=\"0\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:unsignedShort\"\>

     \<xs:minInclusive value=\"1\"/\>

     \<xs:maxInclusive value=\"127\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element name=\"MonthOfYear\" minOccurs=\"0\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:unsignedByte\"\>

     \<xs:minInclusive value=\"1\"/\>

     \<xs:maxInclusive value=\"12\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element name=\"Until\" type=\"xs:string\" minOccurs=\"0\"/\>

     \<xs:element name=\"DayOfMonth\" minOccurs=\"0\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:unsignedByte\"\>

     \<xs:minInclusive value=\"1\"/\>

     \<xs:maxInclusive value=\"31\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element name=\"CalendarType\" minOccurs=\"0\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:unsignedByte\"\>

     \<xs:minInclusive value=\"0\"/\>

     \<xs:maxInclusive value=\"23\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element name=\"IsLeapMonth\" minOccurs=\"0\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:unsignedByte\"\>

     \<xs:minInclusive value=\"0\"/\>

     \<xs:maxInclusive value=\"1\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element name=\"FirstDayOfWeek\" minOccurs=\"0\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:unsignedByte\"\>

     \<xs:minInclusive value=\"0\"/\>

     \<xs:maxInclusive value=\"6\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \</xs:all\>

     \</xs:complexType\>

     \</xs:element\>

     \<xs:element name=\"Exceptions\"\>

     \<xs:complexType\>

     \<xs:sequence minOccurs=\"0\"\>

     \<xs:element name=\"Exception\" maxOccurs=\"1000\"\>

     \<xs:complexType\>

     \<xs:all\>

     \<xs:element name=\"Deleted\" type=\"xs:unsignedByte\" minOccurs=\"0\"/\>

     \<xs:element name=\"ExceptionStartTime\" type=\"xs:string\" minOccurs=\"0\"/\>

     \<xs:element ref=\"Subject\" minOccurs=\"0\"/\>

     \<xs:element ref=\"StartTime\" minOccurs=\"0\"/\>

     \<xs:element ref=\"EndTime\" minOccurs=\"0\"/\>

     \<xs:element ref=\"airsyncbase:Body\" minOccurs=\"0\"/\>

     \<xs:element ref=\"Location\" minOccurs=\"0\"/\>

     \<xs:element ref=\"airsyncbase:Location\" minOccurs=\"0\"/\>

     \<xs:element ref=\"airsyncbase:Attachments\" minOccurs=\"0\"/\>

     \<xs:element ref=\"Categories\" minOccurs=\"0\"/\>

     \<xs:element ref=\"Sensitivity\" minOccurs=\"0\"/\>

     \<xs:element ref=\"BusyStatus\" minOccurs=\"0\"/\>

     \<xs:element ref=\"AllDayEvent\" minOccurs=\"0\"/\>

     \<xs:element ref=\"Reminder\" minOccurs=\"0\"/\>

     \<xs:element ref=\"DtStamp\" minOccurs=\"0\"/\>

     \<xs:element ref=\"airsyncbase:InstanceId\" minOccurs=\"0\"/\> \<xs:element ref=\"MeetingStatus\" minOccurs=\"0\"/\>

     \<xs:element ref=\"Attendees\" minOccurs=\"0\"/\>

     \<xs:element ref=\"AppointmentReplyTime\" minOccurs=\"0\"/\>

     \<xs:element ref=\"ResponseType\" minOccurs=\"0\"/\>

     \<xs:element ref=\"OnlineMeetingConfLink\" minOccurs=\"0\"/\>

     \<xs:element ref=\"OnlineMeetingExternalLink\" minOccurs=\"0\"/\>

     \</xs:all\>

     \</xs:complexType\>

     \</xs:element\>

     \</xs:sequence\>

     \</xs:complexType\>

     \</xs:element\>

     \<xs:element name=\"ResponseRequested\" type=\"xs:boolean\"/\>

     \<xs:element name=\"AppointmentReplyTime\" type=\"xs:string\"/\>

     \<xs:element name=\"ResponseType\" type=\"xs:unsignedInt\"/\>

     \<xs:element name=\"DisallowNewTimeProposal\" type=\"xs:boolean\"/\>

     \<xs:element name=\"OnlineMeetingConfLink\" type=\"xs:string\"/\>

     \<xs:element name=\"OnlineMeetingExternalLink\" type=\"xs:string\"/\>

     \<xs:group name=\"AllProps\"\>

     \<xs:sequence\>

     \<xs:choice maxOccurs=\"unbounded\"\>

     \<xs:element ref=\"Timezone\"/\>

     \<xs:element ref=\"AllDayEvent\"/\>

     \<xs:element ref=\"Body\"/\>

     \<xs:element ref=\"BodyTruncated\"/\>

     \<xs:element ref=\"BusyStatus\"/\>

     \<xs:element ref=\"OrganizerName\"/\>

     \<xs:element ref=\"OrganizerEmail\"/\>

     \<xs:element ref=\"DtStamp\"/\>

     \<xs:element ref=\"EndTime\"/\>

     \<xs:element ref=\"Location\"/\>

     \<xs:element ref=\"Reminder\"/\>

     \<xs:element ref=\"Sensitivity\"/\>

     \<xs:element ref=\"Subject\"/\>

     \<xs:element ref=\"StartTime\"/\>

     \<xs:element ref=\"UID\"/\>

     \<xs:element ref=\"MeetingStatus\"/\>

     \<xs:element ref=\"Attendees\"/\>

     \<xs:element ref=\"Categories\"/\>

     \<xs:element ref=\"Recurrence\"/\>

     \<xs:element ref=\"Exceptions\"/\>

     \<xs:element ref=\"ResponseRequested\"/\>

     \<xs:element ref=\"AppointmentReplyTime\"/\>

     \<xs:element ref=\"ResponseType\"/\>

     \<xs:element ref=\"DisallowNewTimeProposal\"/\>

     \<xs:element ref=\"OnlineMeetingConfLink\"/\>

     \<xs:element ref=\"OnlineMeetingExternalLink\"/\>

     \</xs:choice\>

     \</xs:sequence\>

     \</xs:group\>

     \<xs:group name=\"GhostingProps\"\>

     \<xs:sequence\>

     \<xs:choice maxOccurs=\"unbounded\"\>

     \<xs:element name=\"Timezone\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"AllDayEvent\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"BusyStatus\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"OrganizerName\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"OrganizerEmail\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"DtStamp\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"EndTime\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Location\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Reminder\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Sensitivity\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Subject\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"StartTime\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"UID\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"MeetingStatus\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Attendees\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Categories\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Recurrence\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Exceptions\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"DisallowNewTimeProposal\"

     type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"ResponseRequested\" type=\"airsyncbase:EmptyTag\"/\>

     \</xs:choice\>

     \</xs:sequence\>

     \</xs:group\>

     \<xs:group name=\"TopLevelSchemaProps\"\>

     \<xs:sequence\>

     \<xs:choice maxOccurs=\"unbounded\"\>

     \<xs:element name=\"Timezone\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"StartTime\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"EndTime\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Subject\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Location\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Reminder\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"AllDayEvent\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"BusyStatus\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Recurrence\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Sensitivity\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"DtStamp\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Attendees\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Categories\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"MeetingStatus\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"OrganizerName\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"OrganizerEmail\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"UID\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"DisallowNewTimeProposal\"

     type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"ResponseRequested\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Exceptions\" type=\"airsyncbase:EmptyTag\"/\>

     \</xs:choice\>

     \</xs:sequence\>

     \</xs:group\>

     \</xs:schema\>

# Appendix B: Product Behavior

The information in this specification is applicable to the following Microsoft products or supplemental software. References to product versions include updates to those products.

-   Microsoft Exchange Server 2007 Service Pack 1 (SP1)

```{=html}
<!-- -->
```
-   Microsoft Exchange Server 2010

-   Microsoft Exchange Server 2013

-   Microsoft Exchange Server 2016

-   Microsoft Exchange Server 2019

-   Windows Communication Apps

-   Windows 10 operating system

-   Windows Server 2016 operating system

```{=html}
<!-- -->
```
-   Windows 11 operating system

-   Microsoft Exchange Server Subscription Edition

Exceptions, if any, are noted in this section. If an update version, service pack or Knowledge Base (KB) number appears with a product name, the behavior changed in that update. The new behavior also applies to subsequent updates unless otherwise specified. If a product edition appears with the product version, behavior is different in that product edition.

Unless otherwise specified, any statement of optional behavior in this specification that is prescribed using the terms \"SHOULD\" or \"SHOULD NOT\" implies product behavior in accordance with the SHOULD or SHOULD NOT prescription. Unless otherwise specified, the term \"MAY\" implies that the product does not follow the prescription.

[\<1\> Section 2.2.2.10](\l): Microsoft Exchange Server 2013 Service Pack 1 (SP1) returns a value of 0 when a client specifies a value of 1 (Gregorian).

[\<2\> Section 2.2.2.25](\l): If Interval is set to 0 in command request, Exchange 2007 SP1 and Exchange 2010 return Status value 6; Microsoft Exchange Server 2010 Service Pack 1 (SP1), Exchange 2013, Exchange 2016 and Exchange 2019 return Interval value 1.

[\<3\> Section 2.2.2.37.1](\l): In Exchange 2007 SP1, the **DayOfWeek** element is not supported when the **Type** element is set to zero (0).

[\<4\> Section 2.2.2.46](\l): If the **UID** element is not included in the command request, Exchange 2007 SP1 creates a value and includes it in the command response.

[\<5\> Section 3.1.5.3.1](\l): Exchange 2007 SP1 does not support deleting elements of a recurring calendar item in an **Exception** element.

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
  [7](#Section_ed6ac62c3f6e485da9c88c918fbdb826) Appendix B: Product Behavior   Updated list of supported products.   Major

  ------------------------------------------------------------------------------------------------------------------------------------

# Index

A

Abstract data model

[client](#abstract-data-model) 58

[server](#abstract-data-model-1) 60

[Applicability](#applicability-statement) 9

C

[Capability negotiation](#versioning-and-capability-negotiation) 9

[Change tracking](#change-tracking) 81

Client

[abstract data model](#abstract-data-model) 58

[initialization](#initialization) 58

[message processing](#message-processing-events-and-sequencing-rules) 59

[other local events](#other-local-events) 60

[sequencing rules](#message-processing-events-and-sequencing-rules) 59

[timer events](#timer-events) 60

[timers](#timers) 58

[Creating recurring calendar items example](#creating-recurring-calendar-items) 70

D

Data model - abstract

[client](#abstract-data-model) 58

[server](#abstract-data-model-1) 60

E

Elements

[AllDayEvent](#alldayevent) 14

[AppointmentReplyTime](#appointmentreplytime) 15

[Attendee](#attendee) 16

[Attendees](#attendees) 16

[AttendeeStatus](#attendeestatus) 17

[AttendeeType](#attendeetype) 18

[Body](#body-airsyncbase-namespace) 19

[BusyStatus](#busystatus) 21

[CalendarType](#calendartype) 22

[Categories](#categories) 24

[Category](#category) 24

[ClientUid](#clientuid) 25

[DayOfMonth](#dayofmonth) 26

[DayOfWeek](#dayofweek) 26

[Deleted](#deleted) 27

[DisallowNewTimeProposal](#disallownewtimeproposal) 28

[DtStamp](#dtstamp) 29

[Email](#email) 30

[EndTime](#endtime) 30

[Exception](#exception) 31

[Exceptions](#exceptions) 33

[ExceptionStartTime](#exceptionstarttime) 33

[FirstDayOfWeek](#firstdayofweek) 34

[Interval](#interval) 35

[IsLeapMonth](#isleapmonth) 36

[Location](#location) 37

[MeetingStatus](#meetingstatus) 37

[MonthOfYear](#monthofyear) 39

[Name](#name) 40

[NativeBodyType](#nativebodytype) 40

[Occurrences](#occurrences) 41

[OnlineMeetingConfLink](#onlinemeetingconflink) 42

[OnlineMeetingExternalLink](#onlinemeetingexternallink) 42

[OrganizerEmail](#organizeremail) 43

[OrganizerName](#organizername) 44

[Recurrence](#recurrence) 44

[Reminder](#reminder) 47

[ResponseRequested](#responserequested) 48

[ResponseType](#responsetype) 48

[Sensitivity](#sensitivity) 49

[StartTime](#starttime) 50

[Subject](#subject) 51

[TimeZone](#timezone) 52

[Type](#type) 53

[UID](#uid) 53

[Until](#until) 54

[WeekOfMonth](#weekofmonth) 55

[Elements message](#elements) 10

Examples

[creating recurring calendar items](#creating-recurring-calendar-items) 70

[recurrence patterns that resolve to the same recurring calendar item](#recurrence-patterns-that-resolve-to-the-same-recurring-calendar-item) 72

[setting attendee status from the server](#setting-attendee-status-from-the-server) 68

[synchronizing calendar data](#synchronizing-calendar-data) 65

[synchronizing recurring appointments with exceptions](#synchronizing-recurring-appointments-with-exceptions) 67

F

[Fields - vendor-extensible](#vendor-extensible-fields) 9

[Full XML schema](#appendix-a-full-xml-schema) 75

[XML schema](#appendix-a-full-xml-schema) 75

G

[Glossary](#glossary) 7

[Groups message](#groups) 56

I

[Implementer - security considerations](#security-considerations-for-implementers) 74

[Index of security parameters](#index-of-security-parameters) 74

[Informative references](#informative-references) 9

Initialization

[client](#initialization) 58

[server](#initialization-1) 60

[Introduction](#introduction) 7

M

Message processing

[client](#message-processing-events-and-sequencing-rules) 59

[server](#message-processing-events-and-sequencing-rules-1) 61

Messages

[Elements](#elements) 10

[Groups](#groups) 56

[Namespaces](#namespaces) 10

[syntax](#message-syntax) 10

[transport](#transport) 10

N

[Namespaces message](#namespaces) 10

[Normative references](#normative-references) 8

O

Other local events

[client](#other-local-events) 60

[server](#other-local-events-1) 64

[Overview (synopsis)](#overview) 9

P

[Parameters - security index](#index-of-security-parameters) 74

[Preconditions](#prerequisitespreconditions) 9

[Prerequisites](#prerequisitespreconditions) 9

[Product behavior](#appendix-b-product-behavior) 80

R

[Recurrence patterns that resolve to the same recurring calendar item example](#recurrence-patterns-that-resolve-to-the-same-recurring-calendar-item) 72

[References](#references) 8

[informative](#informative-references) 9

[normative](#normative-references) 8

[Relationship to other protocols](#relationship-to-other-protocols) 9

S

Security

[implementer considerations](#security-considerations-for-implementers) 74

[parameter index](#index-of-security-parameters) 74

Sequencing rules

[client](#message-processing-events-and-sequencing-rules) 59

[server](#message-processing-events-and-sequencing-rules-1) 61

Server

[abstract data model](#abstract-data-model-1) 60

[initialization](#initialization-1) 60

[message processing](#message-processing-events-and-sequencing-rules-1) 61

[other local events](#other-local-events-1) 64

[sequencing rules](#message-processing-events-and-sequencing-rules-1) 61

[timer events](#timer-events-1) 64

[timers](#timers-1) 60

[Setting attendee status from the server example](#setting-attendee-status-from-the-server) 68

[Standards assignments](#standards-assignments) 9

[Synchronizing calendar data example](#synchronizing-calendar-data) 65

[Synchronizing recurring appointments with exceptions example](#synchronizing-recurring-appointments-with-exceptions) 67

T

Timer events

[client](#timer-events) 60

[server](#timer-events-1) 64

Timers

[client](#timers) 58

[server](#timers-1) 60

[Tracking changes](#change-tracking) 81

[Transport](#transport) 10

V

[Vendor-extensible fields](#vendor-extensible-fields) 9

[Versioning](#versioning-and-capability-negotiation) 9

X

[XML schema](#appendix-a-full-xml-schema) 75
