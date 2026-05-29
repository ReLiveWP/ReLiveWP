**\[MS-ASEMAIL\]:**

**Exchange ActiveSync: Email Class Protocol**

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

  2/4/2009     1.0.1              Editorial        Revised and edited technical content.

  3/4/2009     1.0.2              Editorial        Revised and edited technical content.

  4/10/2009    2.0.0              Major            Updated technical content and applicable product releases.

  7/15/2009    3.0.0              Major            Revised and edited for technical content.

  11/4/2009    4.0.0              Major            Updated and revised the technical content.

  2/10/2010    5.0.0              Major            Updated and revised the technical content.

  5/5/2010     6.0.0              Major            Updated and revised the technical content.

  8/4/2010     7.0                Major            Significantly changed the technical content.

  11/3/2010    7.1                Minor            Clarified the meaning of the technical content.

  3/18/2011    7.2                Minor            Clarified the meaning of the technical content.

  8/5/2011     8.0                Major            Significantly changed the technical content.

  10/7/2011    8.0                None             No changes to the meaning, language, or formatting of the technical content.

  1/20/2012    9.0                Major            Significantly changed the technical content.

  4/27/2012    9.0                None             No changes to the meaning, language, or formatting of the technical content.

  7/16/2012    10.0               Major            Significantly changed the technical content.

  10/8/2012    10.1               Minor            Clarified the meaning of the technical content.

  2/11/2013    10.1               None             No changes to the meaning, language, or formatting of the technical content.

  7/26/2013    11.0               Major            Significantly changed the technical content.

  11/18/2013   11.0               None             No changes to the meaning, language, or formatting of the technical content.

  2/10/2014    11.0               None             No changes to the meaning, language, or formatting of the technical content.

  4/30/2014    12.0               Major            Significantly changed the technical content.

  7/31/2014    12.1               Minor            Clarified the meaning of the technical content.

  10/30/2014   13.0               Major            Significantly changed the technical content.

  5/26/2015    14.0               Major            Significantly changed the technical content.

  6/30/2015    15.0               Major            Significantly changed the technical content.

  9/14/2015    16.0               Major            Significantly changed the technical content.

  6/9/2016     17.0               Major            Significantly changed the technical content.

  2/28/2017    18.0               Major            Significantly changed the technical content.

  4/18/2017    18.0               None             No changes to the meaning, language, or formatting of the technical content.

  9/19/2017    18.1               Minor            Clarified the meaning of the technical content.

  12/12/2017   18.1               None             No changes to the meaning, language, or formatting of the technical content.

  7/24/2018    19.0               Major            Significantly changed the technical content.

  10/1/2018    20.0               Major            Significantly changed the technical content.

  11/16/2021   20.1               Minor            Clarified the meaning of the technical content.

  4/29/2022    21.0               Major            Significantly changed the technical content.

  5/20/2025    22.0               Major            Significantly changed the technical content.
  -------------------------------------------------------------------------------------------------------------------------------

Table of Contents

[1 Introduction [8](#introduction)](#introduction)

[1.1 Glossary [8](#glossary)](#glossary)

[1.2 References [10](#references)](#references)

[1.2.1 Normative References [10](#normative-references)](#normative-references)

[1.2.2 Informative References [11](#informative-references)](#informative-references)

[1.3 Overview [11](#overview)](#overview)

[1.4 Relationship to Other Protocols [11](#relationship-to-other-protocols)](#relationship-to-other-protocols)

[1.5 Prerequisites/Preconditions [12](#prerequisitespreconditions)](#prerequisitespreconditions)

[1.6 Applicability Statement [12](#applicability-statement)](#applicability-statement)

[1.7 Versioning and Capability Negotiation [12](#versioning-and-capability-negotiation)](#versioning-and-capability-negotiation)

[1.8 Vendor-Extensible Fields [12](#vendor-extensible-fields)](#vendor-extensible-fields)

[1.9 Standards Assignments [12](#standards-assignments)](#standards-assignments)

[2 Messages [13](#messages)](#messages)

[2.1 Transport [13](#transport)](#transport)

[2.2 Message Syntax [13](#message-syntax)](#message-syntax)

[2.2.1 Namespaces [13](#namespaces)](#namespaces)

[2.2.2 Elements [13](#elements)](#elements)

[2.2.2.1 AccountId [17](#accountid)](#accountid)

[2.2.2.2 AllDayEvent [18](#alldayevent)](#alldayevent)

[2.2.2.3 Attachment [19](#attachment)](#attachment)

[2.2.2.4 Attachments [19](#attachments)](#attachments)

[2.2.2.4.1 Attachments (AirSyncBase Namespace) [19](#attachments-airsyncbase-namespace)](#attachments-airsyncbase-namespace)

[2.2.2.4.2 Attachments (Email Namespace) [20](#attachments-email-namespace)](#attachments-email-namespace)

[2.2.2.5 AttMethod [21](#attmethod)](#attmethod)

[2.2.2.6 AttName [22](#attname)](#attname)

[2.2.2.7 AttOid [22](#attoid)](#attoid)

[2.2.2.8 AttSize [23](#attsize)](#attsize)

[2.2.2.9 Bcc [23](#bcc)](#bcc)

[2.2.2.10 Body [24](#body)](#body)

[2.2.2.10.1 Body (AirSyncBase Namespace) [24](#body-airsyncbase-namespace)](#body-airsyncbase-namespace)

[2.2.2.10.2 Body (Email Namespace) [25](#body-email-namespace)](#body-email-namespace)

[2.2.2.11 BodyPart [26](#bodypart)](#bodypart)

[2.2.2.12 BodySize [26](#bodysize)](#bodysize)

[2.2.2.13 BodyTruncated [27](#bodytruncated)](#bodytruncated)

[2.2.2.14 BusyStatus [28](#busystatus)](#busystatus)

[2.2.2.15 CalendarType [28](#calendartype)](#calendartype)

[2.2.2.16 Categories [30](#categories)](#categories)

[2.2.2.17 Category [30](#category)](#category)

[2.2.2.18 Cc [31](#cc)](#cc)

[2.2.2.19 CompleteTime [31](#completetime)](#completetime)

[2.2.2.20 ContentClass [32](#contentclass)](#contentclass)

[2.2.2.21 ConversationId [33](#conversationid)](#conversationid)

[2.2.2.22 ConversationIndex [33](#conversationindex)](#conversationindex)

[2.2.2.23 DateCompleted [34](#datecompleted)](#datecompleted)

[2.2.2.24 DateReceived [35](#datereceived)](#datereceived)

[2.2.2.25 DayOfMonth [35](#dayofmonth)](#dayofmonth)

[2.2.2.26 DayOfWeek [36](#dayofweek)](#dayofweek)

[2.2.2.27 DisallowNewTimeProposal [37](#disallownewtimeproposal)](#disallownewtimeproposal)

[2.2.2.28 DisplayName [37](#displayname)](#displayname)

[2.2.2.29 DisplayTo [38](#displayto)](#displayto)

[2.2.2.30 DtStamp [39](#dtstamp)](#dtstamp)

[2.2.2.31 DueDate [39](#duedate)](#duedate)

[2.2.2.32 EndTime [40](#endtime)](#endtime)

[2.2.2.33 FirstDayOfWeek [40](#firstdayofweek)](#firstdayofweek)

[2.2.2.34 Flag [41](#flag)](#flag)

[2.2.2.35 FlagType [42](#flagtype)](#flagtype)

[2.2.2.36 From [43](#from)](#from)

[2.2.2.37 GlobalObjId [44](#globalobjid)](#globalobjid)

[2.2.2.38 Importance [45](#importance)](#importance)

[2.2.2.39 InstanceType [46](#instancetype)](#instancetype)

[2.2.2.40 InternetCPID [47](#internetcpid)](#internetcpid)

[2.2.2.41 Interval [47](#interval)](#interval)

[2.2.2.42 IsDraft [48](#isdraft)](#isdraft)

[2.2.2.43 IsLeapMonth [48](#isleapmonth)](#isleapmonth)

[2.2.2.44 LastVerbExecuted [49](#lastverbexecuted)](#lastverbexecuted)

[2.2.2.45 LastVerbExecutionTime [50](#lastverbexecutiontime)](#lastverbexecutiontime)

[2.2.2.46 Location [50](#location)](#location)

[2.2.2.47 MeetingMessageType [51](#meetingmessagetype)](#meetingmessagetype)

[2.2.2.48 MeetingRequest [52](#meetingrequest)](#meetingrequest)

[2.2.2.49 MessageClass [53](#messageclass)](#messageclass)

[2.2.2.50 MIMEData [55](#mimedata)](#mimedata)

[2.2.2.51 MIMESize [56](#mimesize)](#mimesize)

[2.2.2.52 MIMETruncated [57](#mimetruncated)](#mimetruncated)

[2.2.2.53 MonthOfYear [57](#monthofyear)](#monthofyear)

[2.2.2.54 NativeBodyType [58](#nativebodytype)](#nativebodytype)

[2.2.2.55 Occurrences [58](#occurrences)](#occurrences)

[2.2.2.56 OrdinalDate [59](#ordinaldate)](#ordinaldate)

[2.2.2.57 Organizer [60](#organizer)](#organizer)

[2.2.2.58 Read [60](#read)](#read)

[2.2.2.59 ReceivedAsBcc [61](#receivedasbcc)](#receivedasbcc)

[2.2.2.60 Recurrence [61](#recurrence)](#recurrence)

[2.2.2.61 RecurrenceId [62](#recurrenceid)](#recurrenceid)

[2.2.2.62 Recurrences [63](#recurrences)](#recurrences)

[2.2.2.63 Reminder [64](#reminder)](#reminder)

[2.2.2.64 ReminderSet [64](#reminderset)](#reminderset)

[2.2.2.65 ReminderTime [65](#remindertime)](#remindertime)

[2.2.2.66 ReplyTo [65](#replyto)](#replyto)

[2.2.2.67 ResponseRequested [66](#responserequested)](#responserequested)

[2.2.2.68 RightsManagementLicense [67](#rightsmanagementlicense)](#rightsmanagementlicense)

[2.2.2.69 Send [67](#send)](#send)

[2.2.2.70 Sender [68](#sender)](#sender)

[2.2.2.71 Sensitivity [69](#sensitivity)](#sensitivity)

[2.2.2.72 StartDate [69](#startdate)](#startdate)

[2.2.2.73 StartTime [70](#starttime)](#starttime)

[2.2.2.74 Status [71](#status)](#status)

[2.2.2.75 Subject [72](#subject)](#subject)

[2.2.2.75.1 Subject (Email Namespace) [72](#subject-email-namespace)](#subject-email-namespace)

[2.2.2.75.2 Subject (Tasks Namespace) [72](#subject-tasks-namespace)](#subject-tasks-namespace)

[2.2.2.76 SubOrdinalDate [73](#subordinaldate)](#subordinaldate)

[2.2.2.77 ThreadTopic [73](#threadtopic)](#threadtopic)

[2.2.2.78 TimeZone [74](#timezone)](#timezone)

[2.2.2.79 To [75](#to)](#to)

[2.2.2.80 Type [75](#type)](#type)

[2.2.2.81 UmAttDuration [76](#umattduration)](#umattduration)

[2.2.2.82 UmAttOrder [77](#umattorder)](#umattorder)

[2.2.2.83 UmCallerID [77](#umcallerid)](#umcallerid)

[2.2.2.84 UmUserNotes [78](#umusernotes)](#umusernotes)

[2.2.2.85 Until [79](#until)](#until)

[2.2.2.86 UtcDueDate [80](#utcduedate)](#utcduedate)

[2.2.2.87 UtcStartDate [81](#utcstartdate)](#utcstartdate)

[2.2.2.88 WeekOfMonth [82](#weekofmonth)](#weekofmonth)

[2.2.3 Groups [82](#groups)](#groups)

[2.2.3.1 TopLevelSchemaProps [82](#toplevelschemaprops)](#toplevelschemaprops)

[3 Protocol Details [84](#protocol-details)](#protocol-details)

[3.1 Client Details [84](#client-details)](#client-details)

[3.1.1 Abstract Data Model [84](#abstract-data-model)](#abstract-data-model)

[3.1.2 Timers [84](#timers)](#timers)

[3.1.3 Initialization [84](#initialization)](#initialization)

[3.1.4 Higher-Layer Triggered Events [84](#higher-layer-triggered-events)](#higher-layer-triggered-events)

[3.1.4.1 Synchronizing E-Mail Data Between Client and Server [84](#synchronizing-e-mail-data-between-client-and-server)](#synchronizing-e-mail-data-between-client-and-server)

[3.1.4.2 Sending E-Mail [84](#sending-e-mail)](#sending-e-mail)

[3.1.4.3 Searching a Server for E-Mail Data [84](#searching-a-server-for-e-mail-data)](#searching-a-server-for-e-mail-data)

[3.1.4.4 Retrieving Data for One or More E-Mail Items [84](#retrieving-data-for-one-or-more-e-mail-items)](#retrieving-data-for-one-or-more-e-mail-items)

[3.1.4.5 Sending and Receiving Meeting Requests [85](#sending-and-receiving-meeting-requests)](#sending-and-receiving-meeting-requests)

[3.1.4.6 Updating E-Mail Flags on the Server [85](#updating-e-mail-flags-on-the-server)](#updating-e-mail-flags-on-the-server)

[3.1.4.7 Determining Whether a Meeting Request Corresponds to an Existing Calendar Object [85](#determining-whether-a-meeting-request-corresponds-to-an-existing-calendar-object)](#determining-whether-a-meeting-request-corresponds-to-an-existing-calendar-object)

[3.1.5 Message Processing Events and Sequencing Rules [86](#message-processing-events-and-sequencing-rules)](#message-processing-events-and-sequencing-rules)

[3.1.5.1 Find Command Request [86](#find-command-request)](#find-command-request)

[3.1.5.2 ItemOperations Command Request [86](#itemoperations-command-request)](#itemoperations-command-request)

[3.1.5.3 Search Command Request [87](#search-command-request)](#search-command-request)

[3.1.5.4 Sync Command Request [87](#sync-command-request)](#sync-command-request)

[3.1.5.4.1 Updating E-Mail Flags [88](#updating-e-mail-flags)](#updating-e-mail-flags)

[3.1.6 Timer Events [89](#timer-events)](#timer-events)

[3.1.7 Other Local Events [89](#other-local-events)](#other-local-events)

[3.2 Server Details [89](#server-details)](#server-details)

[3.2.1 Abstract Data Model [89](#abstract-data-model-1)](#abstract-data-model-1)

[3.2.2 Timers [89](#timers-1)](#timers-1)

[3.2.3 Initialization [89](#initialization-1)](#initialization-1)

[3.2.4 Higher-Layer Triggered Events [90](#higher-layer-triggered-events-1)](#higher-layer-triggered-events-1)

[3.2.4.1 Synchronizing E-Mail Data Between Client and Server [90](#synchronizing-e-mail-data-between-client-and-server-1)](#synchronizing-e-mail-data-between-client-and-server-1)

[3.2.4.2 Searching for E-Mail Data [90](#searching-for-e-mail-data)](#searching-for-e-mail-data)

[3.2.4.3 Retrieving Data for One or More E-Mail Items [90](#retrieving-data-for-one-or-more-e-mail-items-1)](#retrieving-data-for-one-or-more-e-mail-items-1)

[3.2.5 Message Processing Events and Sequencing Rules [90](#message-processing-events-and-sequencing-rules-1)](#message-processing-events-and-sequencing-rules-1)

[3.2.5.1 Find Command Response [90](#find-command-response)](#find-command-response)

[3.2.5.2 ItemOperations Command Response [90](#itemoperations-command-response)](#itemoperations-command-response)

[3.2.5.3 Search Command Response [90](#search-command-response)](#search-command-response)

[3.2.5.4 Sync Command Response [91](#sync-command-response)](#sync-command-response)

[3.2.5.4.1 Sending E-Mail Changes to the Client [91](#sending-e-mail-changes-to-the-client)](#sending-e-mail-changes-to-the-client)

[3.2.5.4.2 Updating E-Mail Flags [93](#updating-e-mail-flags-1)](#updating-e-mail-flags-1)

[3.2.6 Timer Events [94](#timer-events-1)](#timer-events-1)

[3.2.7 Other Local Events [95](#other-local-events-1)](#other-local-events-1)

[4 Protocol Examples [96](#protocol-examples)](#protocol-examples)

[4.1 Synchronizing E-Mail [96](#synchronizing-e-mail)](#synchronizing-e-mail)

[4.1.1 Synchronizing Only E-Mail Metadata [96](#synchronizing-only-e-mail-metadata)](#synchronizing-only-e-mail-metadata)

[4.1.2 Synchronizing E-Mail Metadata and Body [97](#synchronizing-e-mail-metadata-and-body)](#synchronizing-e-mail-metadata-and-body)

[4.1.3 Synchronizing E-Mail Attachments [98](#synchronizing-e-mail-attachments)](#synchronizing-e-mail-attachments)

[4.1.3.1 Synchronizing an E-Mail with an Electronic Voice Mail Attachment [99](#synchronizing-an-e-mail-with-an-electronic-voice-mail-attachment)](#synchronizing-an-e-mail-with-an-electronic-voice-mail-attachment)

[4.1.3.2 Synchronizing an E-mail with a Text Attachment [99](#synchronizing-an-e-mail-with-a-text-attachment)](#synchronizing-an-e-mail-with-a-text-attachment)

[4.1.4 Deleting an E-Mail [100](#deleting-an-e-mail)](#deleting-an-e-mail)

[4.1.5 Synchronizing Meeting Requests [101](#synchronizing-meeting-requests)](#synchronizing-meeting-requests)

[4.1.5.1 Synchronizing a Non-Recurring Meeting Request [101](#synchronizing-a-non-recurring-meeting-request)](#synchronizing-a-non-recurring-meeting-request)

[4.1.5.2 Synchronizing a Recurring Meeting Request [102](#synchronizing-a-recurring-meeting-request)](#synchronizing-a-recurring-meeting-request)

[4.1.6 Retrieving E-Mail Metadata and Body [103](#retrieving-e-mail-metadata-and-body)](#retrieving-e-mail-metadata-and-body)

[4.2 Setting the Flag on an E-Mail [105](#setting-the-flag-on-an-e-mail)](#setting-the-flag-on-an-e-mail)

[4.2.1 Setting a Flag [105](#setting-a-flag)](#setting-a-flag)

[4.2.2 Marking a Flag as Complete [106](#marking-a-flag-as-complete)](#marking-a-flag-as-complete)

[4.2.3 Clearing a Flag [107](#clearing-a-flag)](#clearing-a-flag)

[4.3 Converting a GlobalObjId to a UID [107](#converting-a-globalobjid-to-a-uid)](#converting-a-globalobjid-to-a-uid)

[4.4 Adding a Draft Email with Attachments [108](#adding-a-draft-email-with-attachments)](#adding-a-draft-email-with-attachments)

[5 Security [111](#security)](#security)

[5.1 Security Considerations for Implementers [111](#security-considerations-for-implementers)](#security-considerations-for-implementers)

[5.2 Index of Security Parameters [111](#index-of-security-parameters)](#index-of-security-parameters)

[6 Appendix A: Full XML Schema [112](#appendix-a-full-xml-schema)](#appendix-a-full-xml-schema)

[6.1 Email Namespace Schema [112](#email-namespace-schema)](#email-namespace-schema)

[6.2 Email2 Namespace Schema [115](#email2-namespace-schema)](#email2-namespace-schema)

[7 Appendix B: Product Behavior [117](#appendix-b-product-behavior)](#appendix-b-product-behavior)

[8 Change Tracking [118](#change-tracking)](#change-tracking)

[9 Index [119](#index)](#index)

# Introduction

The Exchange ActiveSync: Email Class Protocol enables the communication of e-mail data between a mobile device and the server in the ActiveSync protocol.

Sections 1.5, 1.8, 1.9, 2, and 3 of this specification are normative. All other sections and examples in this specification are informative.

## Glossary

This document uses the following terms:

> []{#gt_24ddbbb4-b79e-4419-96ec-0fdd229c9ebf .anchor}**Augmented Backus-Naur Form (ABNF)**: A modified version of Backus-Naur Form (BNF), commonly used by Internet specifications. ABNF notation balances compactness and simplicity with reasonable representational power. ABNF differs from standard BNF in its definitions and uses of naming rules, repetition, alternatives, order-independence, and value ranges. For more information, see [\[RFC5234\]](https://go.microsoft.com/fwlink/?LinkId=123096).
>
> []{#gt_179b9392-9019-45a3-880b-26f6890522b7 .anchor}**base64 encoding**: A binary-to-text encoding scheme whereby an arbitrary sequence of bytes is converted to a sequence of printable ASCII characters, as described in [\[RFC4648\]](https://go.microsoft.com/fwlink/?LinkId=90487).
>
> []{#gt_ad861812-8cb0-497a-80bb-13c95aa4e425 .anchor}**binary large object (BLOB)**: A discrete packet of data that is stored in a database and is treated as a sequence of uninterpreted bytes.
>
> []{#gt_f5634b00-a1bf-4143-bb4f-9cd9dbad2bc0 .anchor}**blind carbon copy (Bcc) recipient**: An addressee on a Message object that is not visible to recipients of the Message object.
>
> []{#gt_7204b2ed-dcef-4434-be15-6451f92d03fb .anchor}**calendar**: A date range that shows availability, [**meetings**](#gt_cbc56efc-e4f7-4b31-9e5f-9c44e3924d94), and appointments for one or more users or resources. See also [**Calendar object**](#gt_b9ce8e55-dae6-467b-b5dc-850087d4dc18).
>
> []{#gt_60b55610-ca65-41f2-91d8-a4d6f4cc6d20 .anchor}**Calendar folder**: A Folder object that contains [**Calendar objects**](#gt_b9ce8e55-dae6-467b-b5dc-850087d4dc18).
>
> []{#gt_b9ce8e55-dae6-467b-b5dc-850087d4dc18 .anchor}**Calendar object**: A Message object that represents an event, which can be a one-time event or a recurring event. The Calendar object includes properties that specify event details such as description, organizer, date and time, and status.
>
> []{#gt_a805ae0b-0a4e-43ae-b75d-de65a36fa73c .anchor}**clear-signed message**: An Internet email message that is in the format described by [\[RFC1847\]](https://go.microsoft.com/fwlink/?LinkId=193286) and is identified with the media type \"multipart/signed\", or the Message object representing such a message. An important class of clear-signed message, based on a \"multipart/signed\" format, is the S/MIME clear-signed message, as described in [\[RFC5751\]](https://go.microsoft.com/fwlink/?LinkID=194261) and [\[RFC3852\]](https://go.microsoft.com/fwlink/?LinkId=90445).
>
> []{#gt_210637d9-9634-4652-a935-ded3cd434f38 .anchor}**code page**: An ordered set of characters of a specific script in which a numerical index (code-point value) is associated with each character. Code pages are a means of providing support for character sets and keyboard layouts used in different countries. Devices such as the display and keyboard can be configured to use a specific code page and to switch from one code page (such as the United States) to another (such as Portugal) at the user\'s request.
>
> []{#gt_0aec5fa3-827f-4725-9d37-4b5bff86d6e1 .anchor}**conversation**: A single representation of a send/response series of email messages. A conversation appears in the Inbox as one unit and allows the user to view and read the series of related email messages in a single effort.
>
> []{#gt_f2369991-a884-4843-a8fa-1505b6d5ece7 .anchor}**Coordinated Universal Time (UTC)**: A high-precision atomic time standard that approximately tracks Universal Time (UT). It is the basis for legal, civil time all over the Earth. Time zones around the world are expressed as positive and negative offsets from UTC. In this role, it is also referred to as Zulu time (Z) and Greenwich Mean Time (GMT). In these specifications, all references to UTC refer to the time at UTC-0 (or GMT).
>
> []{#gt_eeac1cee-185f-47d9-ace5-555e3a2a6930 .anchor}**delegate**: A user or resource that has permissions to act on behalf of another user or resource.
>
> []{#gt_9d58a6d9-25fe-4093-98bd-f5838ac51a47 .anchor}**delivery receipt**: A report message that is generated and sent by a client or server to the sender of a message or another designated recipient when an email message is received by an intended recipient.
>
> []{#gt_97c27c06-f5e7-4eae-a54e-1839d41f69dc .anchor}**Drafts folder**: A special folder that is the default location for Message objects that have been saved but not sent.
>
> []{#gt_549c4960-e8be-4c24-bc2b-b86530f1c1bf .anchor}**Hypertext Markup Language (HTML)**: An application of the Standard Generalized Markup Language (SGML) that uses tags to mark elements in a document, as described in [\[HTML\]](https://go.microsoft.com/fwlink/?LinkId=89880).
>
> []{#gt_baa08600-0402-47f6-a8ce-9690cf962c96 .anchor}**Inbox folder**: A special folder that is the default location for Message objects received by a user or resource.
>
> []{#gt_d3ad0e15-adc9-4174-bacf-d929b57278b3 .anchor}**mailbox**: A message store that contains email, calendar items, and other Message objects for a single recipient.
>
> []{#gt_cbc56efc-e4f7-4b31-9e5f-9c44e3924d94 .anchor}**meeting**: An event with attendees.
>
> []{#gt_1f032bde-d2f7-4fc8-87d0-090964e7b5a5 .anchor}**message part**: A message body with a string property that contains only the portion of an email message that is original to the message. It does not include any previous, quoted messages. If a message does not quote a previous message, the message part is identical to the message body.
>
> []{#gt_af6ba277-34c1-493d-8103-71d2af36ce30 .anchor}**Multipurpose Internet Mail Extensions (MIME)**: A set of extensions that redefines and expands support for various types of content in email messages, as described in [\[RFC2045\]](https://go.microsoft.com/fwlink/?LinkId=90307), [\[RFC2046\]](https://go.microsoft.com/fwlink/?LinkId=90308), and [\[RFC2047\]](https://go.microsoft.com/fwlink/?LinkId=90309).
>
> []{#gt_2540c3dc-aeea-4d46-bf5a-a019d9e645f5 .anchor}**non-delivery report**: A report message that is generated and sent by a server to the sender of a message if an email message could not be received by an intended recipient.
>
> []{#gt_82b4c00f-7f31-46d6-90e1-459aaf901bd6 .anchor}**non-read receipt**: A message that is generated when an email message is deleted at the expiration of a time limit or due to other client-specific criteria.
>
> []{#gt_171744b8-3f44-4198-b7b9-1c0147282d2c .anchor}**Object Linking and Embedding (OLE)**: A technology for transferring and sharing information between applications by inserting a file or part of a file into a compound document. The inserted file can be either embedded or linked. See also embedded object and linked object.
>
> []{#gt_7be29e6d-10e1-4658-8735-1c4f01f77d1b .anchor}**opaque-signed message**: An Internet email message that is in the format described by \[RFC5751\] and uses the SignedData CMS content type described in \[RFC3852\], or the Message object that represents such a message.
>
> []{#gt_0efee4a8-a2e9-48fe-87f8-d45097de6b72 .anchor}**orphan instance**: An instance of an event that is in a [**recurring series**](#gt_2325d666-e02f-49e4-afa5-3e896d672efe) and is in a Calendar folder without the recurring series. For all practical purposes, this is a single instance.
>
> []{#gt_d4ab6719-b583-467a-a631-95feb7a5ea34 .anchor}**Out of Office (OOF)**: One of the possible values for the free/busy status on an appointment. It indicates that the user will not be in the office during the appointment.
>
> []{#gt_482683b0-5cf4-483f-b41e-162383fbb5ca .anchor}**read receipt**: An email message that is sent to the sender of a message to indicate that a message recipient received the message.
>
> []{#gt_53dfe4f3-05d0-41aa-8217-ecd1962b340b .anchor}**recipient**: An entity that can receive email messages.
>
> []{#gt_4275047f-9935-46db-b9b8-8ca605d16649 .anchor}**recurrence pattern**: Information for a repeating event, such as the start and end time, the number of occurrences, and how occurrences are spaced, such as daily, weekly, or monthly.
>
> []{#gt_2325d666-e02f-49e4-afa5-3e896d672efe .anchor}**recurring series**: An event that repeats at specific intervals of time according to a recurrence pattern.
>
> []{#gt_84bfada5-a327-4110-a257-cffd8fc3fe61 .anchor}**S/MIME (Secure/Multipurpose Internet Mail Extensions)**: A set of cryptographic security services, as described in \[RFC5751\].
>
> []{#gt_78bfb817-fde0-4756-9cae-7c68c5c962f5 .anchor}**tentative**: One of the possible values for the free/busy status on an appointment. A tentative status indicates that the user is tentatively booked during the appointment.
>
> []{#gt_e18af8e8-01d7-4f91-8a1e-0fb21b191f95 .anchor}**Uniform Resource Identifier (URI)**: A string that identifies a resource. The URI is an addressing mechanism defined in Internet Engineering Task Force (IETF) Uniform Resource Identifier (URI): Generic Syntax [\[RFC3986\]](https://go.microsoft.com/fwlink/?LinkId=90453).
>
> []{#gt_f8d6223d-5289-4966-9fc0-8ec7b7b42860 .anchor}**Voice over IP (VoIP)**: The use of the Internet Protocol (IP) for transmitting voice communications. VoIP delivers digitized audio in packet form and can be used to transmit over intranets, extranets, and the Internet.
>
> []{#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc .anchor}**Wireless Application Protocol (WAP) Binary XML (WBXML)**: A compact binary representation of [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) that is designed to reduce the transmission size of XML documents over narrowband communication channels.
>
> []{#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85 .anchor}**XML**: The Extensible Markup Language, as described in [\[XML1.0\]](https://go.microsoft.com/fwlink/?LinkId=90599).
>
> []{#gt_a364f92c-0374-4568-b7f8-40bd74437dd5 .anchor}**XML element**: An [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) structure that typically consists of a start tag, an end tag, and the information between those tags. Elements can have attributes and can contain other elements.
>
> []{#gt_485f05b3-df3b-45ac-b8bf-d05f5d185a24 .anchor}**XML namespace**: A collection of names that is used to identify elements, types, and attributes in XML documents identified in a URI reference \[RFC3986\]. A combination of XML namespace and local name allows XML documents to use elements, types, and attributes that have the same names but come from different sources. For more information, see [\[XMLNS-2ED\]](https://go.microsoft.com/fwlink/?LinkId=90602).
>
> []{#gt_bd0ce6f9-c350-4900-827e-951265294067 .anchor}**XML schema**: A description of a type of XML document that is typically expressed in terms of constraints on the structure and content of documents of that type, in addition to the basic syntax constraints that are imposed by [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) itself. An XML schema provides a view of a document type at a relatively high level of abstraction.
>
> **MAY, SHOULD, MUST, SHOULD NOT, MUST NOT:** These terms (in all caps) are used as defined in [\[RFC2119\]](https://go.microsoft.com/fwlink/?LinkId=90317). All statements of optional behavior use either MAY, SHOULD, or SHOULD NOT.

## References

Links to a document in the Microsoft Open Specifications library point to the correct section in the most recently published version of the referenced document. However, because individual documents in the library are not updated at the same time, the section numbers in the documents may not match. You can confirm the correct section numbering by checking the [Errata](https://go.microsoft.com/fwlink/?linkid=850906).

### Normative References

We conduct frequent surveys of the normative references to assure their continued availability. If you have any issue with finding a normative reference, please contact <dochelp@microsoft.com>. We will assist you in finding the relevant information.

\[E164\] ITU-T, \"The International Public Telecommunication Numbering Plan\", Recommendation E.164, February 2005, [http://www.itu.int/rec/T-REC-E.164/e](https://go.microsoft.com/fwlink/?LinkId=89855)

**Note** There is a charge to download the specification.

\[MS-ASAIRS\] Microsoft Corporation, \"[Exchange ActiveSync: AirSyncBase Namespace Protocol](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c)\".

\[MS-ASCAL\] Microsoft Corporation, \"[Exchange ActiveSync: Calendar Class Protocol](%5bMS-ASCAL%5d.pdf#Section_0c4486824a6a459aae662fed0712bef9)\".

\[MS-ASCMD\] Microsoft Corporation, \"[Exchange ActiveSync: Command Reference Protocol](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a)\".

\[MS-ASCON\] Microsoft Corporation, \"[Exchange ActiveSync: Conversations Protocol](%5bMS-ASCON%5d.pdf#Section_8571bf985f7b4c2fab28c32176d20169)\".

\[MS-ASDTYPE\] Microsoft Corporation, \"[Exchange ActiveSync: Data Types](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3)\".

\[MS-ASHTTP\] Microsoft Corporation, \"[Exchange ActiveSync: HTTP Protocol](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d)\".

\[MS-ASMS\] Microsoft Corporation, \"[Exchange ActiveSync: Short Message Service (SMS) Protocol](%5bMS-ASMS%5d.pdf#Section_3123f34aaabe4ec5aa836f6d48698a8b)\".

\[MS-ASRM\] Microsoft Corporation, \"[Exchange ActiveSync: Rights Management Protocol](%5bMS-ASRM%5d.pdf#Section_71e681b7e1784c1096b678df7fa77dfc)\".

\[MS-ASTASK\] Microsoft Corporation, \"[Exchange ActiveSync: Tasks Class Protocol](%5bMS-ASTASK%5d.pdf#Section_b8fe266450ba4d00bf6be4deab352c89)\".

\[MS-ASWBXML\] Microsoft Corporation, \"[Exchange ActiveSync: WAP Binary XML (WBXML) Algorithm](%5bMS-ASWBXML%5d.pdf#Section_39973eb11e404eb5ac7442781c5a33bc)\".

\[MS-IPFFX\] Microsoft Corporation, \"[InfoPath Form File Format](%5bMS-IPFFX%5d.pdf#Section_18d25c38f26448e0b64dc71ce00b2de4)\".

\[RFC2119\] Bradner, S., \"Key words for use in RFCs to Indicate Requirement Levels\", BCP 14, RFC 2119, March 1997, [https://www.rfc-editor.org/info/rfc2119](https://go.microsoft.com/fwlink/?LinkId=90317)

\[RFC2445\] Dawson, F., and Stenerson, D., \"Internet Calendaring and Scheduling Core Object Specification (iCalendar)\", RFC 2445, November 1998, [http://www.rfc-editor.org/rfc/rfc2445.txt](https://go.microsoft.com/fwlink/?LinkId=112504)

\[RFC2446\] Silverberg, S., Mansour, S., Dawson, F., and Hopson, R., \"iCalendar Transport-Independent Interoperability Protocol (iTIP) Scheduling Events, BusyTime, To-Dos, and Journal Entries\", RFC 2446, November 1998, [http://www.ietf.org/rfc/rfc2446.txt](https://go.microsoft.com/fwlink/?LinkId=90354)

\[RFC2447\] Dawson, F., Mansour, S., and Silverberg, S., \"iCalendar Message-Based Interoperability Protocol (iMIP)\", RFC 2447, November 1998, [http://www.rfc-editor.org/rfc/rfc2447.txt](https://go.microsoft.com/fwlink/?LinkId=193299)

\[RFC3261\] Rosenberg, J., Schulzrinne, H., Camarillo, G., Johnston, A., Peterson, J., Sparks, R., Handley, M., and Schooler, E., \"SIP: Session Initiation Protocol\", RFC 3261, June 2002, [http://www.ietf.org/rfc/rfc3261.txt](https://go.microsoft.com/fwlink/?LinkId=90410)

\[XMLNS\] Bray, T., Hollander, D., Layman, A., et al., Eds., \"Namespaces in XML 1.0 (Third Edition)\", W3C Recommendation, December 2009, [https://www.w3.org/TR/2009/REC-xml-names-20091208/](https://go.microsoft.com/fwlink/?LinkId=191840)

\[XMLSCHEMA1\] Thompson, H., Beech, D., Maloney, M., and Mendelsohn, N., Eds., \"XML Schema Part 1: Structures\", W3C Recommendation, May 2001, [https://www.w3.org/TR/2001/REC-xmlschema-1-20010502/](https://go.microsoft.com/fwlink/?LinkId=90608)

\[XML\] World Wide Web Consortium, \"Extensible Markup Language (XML) 1.0 (Fourth Edition)\", W3C Recommendation 16 August 2006, edited in place 29 September 2006, [http://www.w3.org/TR/2006/REC-xml-20060816/](https://go.microsoft.com/fwlink/?LinkId=90598)

### Informative References

\[MS-OXPROTO\] Microsoft Corporation, \"[Exchange Server Protocols System Overview](%5bMS-OXPROTO%5d.pdf#Section_734ab967e43e425babe1974af56c0283)\".

## Overview

This protocol describes the [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) representation of e-mail message data that is used for client and server communication as described in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a). The e-mail message data is included in protocol command requests when e-mail message data is sent from the client to the server, and is included in protocol command responses when e-mail message data is returned from the server to the client.

## Relationship to Other Protocols

This protocol describes the [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) representation of e-mail message data that is used by the command requests and responses that are described in in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a). The protocol that controls the transmission of these commands between client and server is described in [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d). The [**Wireless Application Protocol (WAP) Binary XML (WBXML)**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc), as described in [\[MS-ASWBXML\]](%5bMS-ASWBXML%5d.pdf#Section_39973eb11e404eb5ac7442781c5a33bc), is used to transmit the XML markup that constitutes the request body and the response body.

All data types in this document conform to the data type definitions that are described in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3). Common [**XML schema**](#gt_bd0ce6f9-c350-4900-827e-951265294067) elements that are used by other classes are defined in [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c).

For conceptual background information and overviews of the relationships and interactions between this and other protocols, see [\[MS-OXPROTO\]](%5bMS-OXPROTO%5d.pdf#Section_734ab967e43e425babe1974af56c0283).

## Prerequisites/Preconditions

None.

## Applicability Statement

This protocol describes a set of [**XML elements**](#gt_a364f92c-0374-4568-b7f8-40bd74437dd5) that are used to communicate e-mail data when using the commands described in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a). This set of elements is applicable when communicating e-mail data such as to, from, and subject, as well as body, attachment, flag, and meeting request information between a mobile device and a server. These elements are not applicable when sending [**calendar**](#gt_7204b2ed-dcef-4434-be15-6451f92d03fb), task, note, or contact data between a mobile device and a server.

## Versioning and Capability Negotiation

None.

## Vendor-Extensible Fields

None.

## Standards Assignments

None.

# Messages

## Transport

This protocol consists of a series of [**XML elements**](#gt_a364f92c-0374-4568-b7f8-40bd74437dd5) that are embedded inside of a command request or response, as specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

The [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) markup that constitutes the request body or the response body that is transmitted between the client and server uses [**Wireless Application Protocol (WAP) Binary XML (WBXML)**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc) as specified in [\[MS-ASWBXML\]](%5bMS-ASWBXML%5d.pdf#Section_39973eb11e404eb5ac7442781c5a33bc).

## Message Syntax

The [**XML schemas**](#gt_bd0ce6f9-c350-4900-827e-951265294067) for the **Email** and **Email2** namespaces are described in section [6](#Section_58a9a783770345d5b559e5db208e0a14).

The markup that is used by this protocol MUST be well-formed [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85), as specified in [\[XML\]](https://go.microsoft.com/fwlink/?LinkId=90598).

### Namespaces

This specification defines and references various [**XML namespaces**](#gt_485f05b3-df3b-45ac-b8bf-d05f5d185a24) using the mechanisms specified in [\[XMLNS\]](https://go.microsoft.com/fwlink/?LinkId=191840). Although this specification associates a specific XML namespace prefix for each XML namespace that is used, the choice of any particular XML namespace prefix is implementation-specific and not significant for interoperability.

  ------------------------------------------------------------------------------------------------------------------------------------------------------
  Prefix           Namespace URI                          Reference
  ---------------- -------------------------------------- ----------------------------------------------------------------------------------------------
  email            **Email**                              

  email2           **Email2**                             

  airsync          **AirSync**                            [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21

  airsyncbase      **AirSyncBase**                        [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c)

  calendar         **Calendar**                           [\[MS-ASCAL\]](%5bMS-ASCAL%5d.pdf#Section_0c4486824a6a459aae662fed0712bef9)

  itemoperations   **ItemOperations**                     \[MS-ASCMD\] section 2.2.1.10

  rm               **RightsManagement**                   [\[MS-ASRM\]](%5bMS-ASRM%5d.pdf#Section_71e681b7e1784c1096b678df7fa77dfc)

  search           **Search**                             \[MS-ASCMD\] section 2.2.1.16

  tasks            **Tasks**                              [\[MS-ASTASK\]](%5bMS-ASTASK%5d.pdf#Section_b8fe266450ba4d00bf6be4deab352c89)

  xs               **http://www.w3.org/2001/XMLSchema**   [\[XMLSCHEMA1\]](https://go.microsoft.com/fwlink/?LinkId=90608)
  ------------------------------------------------------------------------------------------------------------------------------------------------------

### Elements

Elements of the **E-mail** class are defined in five namespaces: **Email**, **Email2**, **AirSyncBase**, **Tasks**, and **RightsManagement**. All elements defined in the **Email** namespace and the **Email2** namespace, as well as the top-level elements imported from the **AirSyncBase** namespace, **Tasks** namespace, and **RightsManagement** namespace, are specified in this document. However, elements defined in the **AirSyncBase** namespace are further specified in [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c).

The elements are defined in the **Email** namespace, except where indicated by the presence of a namespace prefix, as defined in section [2.2.1](#Section_895aaaf078544963b025c3c41ae13cbc). A prefix is used for an element in the **Email** namespace only where necessary to disambiguate the element from another one of the same name.

The following table summarizes the set of common [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) schema elements that are defined by this specification. Elements that are specific to a particular operation are specified further in sections [3.1.5.2](#Section_f83d87c403554adebb31cc1e8df9e1b6), [3.1.5.3](#Section_f479b9297e5b447aa898a8323439f131), [3.1.5.4](#Section_a8602ea5a3f3442683b5a8d5315a953d), [3.2.5.2](#Section_8c041a11353841e2917ae41d5936102a), [3.2.5.3](#Section_f6574aa58d1c4a9291682c9556437c18), and [3.2.5.4](#Section_03e9386ea7c94b1b92fedf15529e20bc).

  -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Element name                                                                                       Description
  -------------------------------------------------------------------------------------------------- ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  **email2:Bcc** (section [2.2.2.9](#Section_e6131f5863a449d5b5922830180fd868))                      Specifies the [**blind carbon copy (Bcc) recipients**](#gt_f5634b00-a1bf-4143-bb4f-9cd9dbad2bc0) of an email.

  **email2:IsDraft** (section [2.2.2.42](#Section_a2d07cb8a58741fc81972c5f641e3f37))                 Specifies whether an email is a draft.

  **email2:Send** (section [2.2.2.69](#Section_51e66d41c2d94dd0bc14311cdddbff35))                    Specifies whether an email is to be saved as a draft or sent.

  **To** (section [2.2.2.79](#Section_86168c1043494946a8bb9ce39fd9bb24))                             Specifies the list of [**recipients**](#gt_53dfe4f3-05d0-41aa-8217-ecd1962b340b).

  **Cc** (section [2.2.2.18](#Section_0a808a7f3e8845ad98814549ca85c008))                             Specifies the list of secondary recipients.

  **From** (section [2.2.2.36](#Section_bf2394e29c1b45ec899b0fba04f7c1e7))                           Specifies the e-mail address of the message sender.

  **Subject** (section [2.2.2.75.1](#Section_cf3cc4bb765b467195095a16a9184779))                      Specifies the subject of the e-mail message.

  **ReplyTo** (section [2.2.2.66](#Section_2a5e10c7591d44899f1976d5af28c383))                        Specifies the e-mail address to which replies will be addressed by default.

  **DateReceived** (section [2.2.2.24](#Section_51c28fa0b2884b85a9e1db5b5a44fc1f))                   Specifies the date and time that the message was received on the server.

  **DisplayTo** (section [2.2.2.29](#Section_237c7acd4e0b465087c43e19989aa22c))                      Specifies the names of the primary recipients of the message.

  **ThreadTopic** (section [2.2.2.77](#Section_623a044774c74145849f8732f8f0efdf))                    Specifies the topic used in [**conversation**](#gt_0aec5fa3-827f-4725-9d37-4b5bff86d6e1) threading.

  **Importance** (section [2.2.2.38](#Section_f98d35bd5638410a83490fec21aea6f7))                     Specifies the importance of the message, as determined by the sender.

  **Read** (section [2.2.2.58](#Section_7dce52172fb147508c5216a709aa02cf))                           Specifies whether the message has been read.

  **airsyncbase:Attachments** (section [2.2.2.4.1](#Section_14e0cbaef1c6499ba64c9293c675ef1c))       Specifies the collection of **airsyncbase:Attachment** elements.

  **Attachments** (section [2.2.2.4.2](#Section_0ffe420083144be29b0d1d3974ff50c6))                   Specifies the collection of **email:Attachment** elements.

  **Attachment** (section [2.2.2.3](#Section_a021a304914546058897b6668e3459d7))                      Specifies the e-mail attachment.

  **AttName** (section [2.2.2.6](#Section_e6eea96a5f7f497386f3c9c23e0919d5))                         Specifies the location of the attachment file to be retrieved from the server.

  **AttSize** (section [2.2.2.8](#Section_1d7f339188e14daab8943e0cc48ec32e))                         Specifies the estimated size, in bytes, of the attachment file.

  **AttOid** (section [2.2.2.7](#Section_f6cf0deab3d849a79632b8674a4a5fe7))                          Specifies the unique identifier of the attachment.

  **AttMethod** (section [2.2.2.5](#Section_ac7ae25bbe524891b148538ee894656d))                       Specifies the method in which the file was attached.

  **DisplayName** (section [2.2.2.28](#Section_8b222a02052e4d42b53c0e41d6d8c891))                    Specifies the name of the attachment file as displayed to the user.

  **email2:UmAttOrder** (section [2.2.2.82](#Section_61072690a3b4441c8b710916192f1b44))              Specifies the order of electronic voice mail attachments.

  **email2:UmAttDuration** (section [2.2.2.81](#Section_f24ac39f7bc1426b9a23f94a3b0f27d5))           Specifies the duration of electronic voicemail attachments.

  **airsyncbase:Body** (section [2.2.2.10.1](#Section_c2e6f8024947446fa0c6a7fb684a37cf))             Specifies a description of the body text, along with its data.

  **Body** (section [2.2.2.10.2](#Section_d836459d3c7842e4ada7c551fdd8bda6))                         Contains the body of an email message that is retrieved from the server.

  **BodySize** (section [2.2.2.12](#Section_39851d9eaa364751a43d2953fe2b6da1))                       Specifies the full size, in characters, of the email message body.

  **BodyTruncated** (section [2.2.2.13](#Section_e327e10687644cabb78863d8f3fc257c))                  Indicates whether the body of the email message was truncated when sent from the server.

  **MIMEData** (section [2.2.2.50](#Section_6d4ca3d1b5424ee189633867089468fb))                       Contains the raw [**Multipurpose Internet Mail Extensions (MIME)**](#gt_af6ba277-34c1-493d-8103-71d2af36ce30) data of an email message that is retrieved from the server.

  **MIMESize** (section [2.2.2.51](#Section_2275c29d592a4ddc97cd336415165eec))                       Specifies either the size, in characters, of the string returned in the **MIMEData** element, if the server returns untruncated MIME data, or the original size, in characters, of the MIME data, if the server returns truncated MIME data.

  **MIMETruncated** (section [2.2.2.52](#Section_ad30e1a89e93424fbda03a473074983b))                  Indicates whether the **MIMEData** element contains truncated data.

  **MessageClass** (section [2.2.2.49](#Section_51d84da6a2da41e98ca7eb6c4e72c28d))                   Specifies the message class of this e-mail message.

  **MeetingRequest** (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df))                 Specifies a meeting request accompanying an e-mail message.

  **AllDayEvent** (section [2.2.2.2](#Section_4ff47c4583b147189114b668be141793))                     Specifies whether the calendar item is an all-day event.

  **StartTime** (section [2.2.2.73](#Section_d05462bb7b18432a8cc132179ad2e0fb))                      Specifies the date and time that the meeting starts.

  **DtStamp** (section [2.2.2.30](#Section_6ec89d167c0546d080d20801a28d9157))                        Specifies the date and time that the calendar item was created.

  **EndTime** (section [2.2.2.32](#Section_41b811f7a98946d0abc158706c150623))                        Specifies the date and time that the meeting ends.

  **InstanceType** (section [2.2.2.39](#Section_a4d4823969364b229267623ecdb49b5e))                   Specifies the type of calendar item.

  **Location** (section [2.2.2.46](#Section_1356b4a405654bda815663165ed1e503))                       Specifies the location for the calendar item.

  **Organizer** (section [2.2.2.57](#Section_d17abe24f69c46d8b7fabf29ee6fb63f))                      Specifies the e-mail alias of the [**meeting**](#gt_cbc56efc-e4f7-4b31-9e5f-9c44e3924d94) organizer.

  **RecurrenceId** (section [2.2.2.61](#Section_262bccb1697e4d2c8146227005bcb4b9))                   Specifies a specific instance of a recurring calendar item.

  **Reminder** (section [2.2.2.63](#Section_8ba415190ee844adbd662a9541f365ae))                       Specifies the number of seconds prior to the calendar item\'s start time that a reminder is displayed.

  **ResponseRequested** (section [2.2.2.67](#Section_eccb418ee5964ce9a5fa138ffe8aa14a))              Specifies whether the originator of the meeting has requested a response.

  **Recurrences** (section [2.2.2.62](#Section_8722b485daac45aea64035241de8233c))                    Specifies a collection of **Recurrence** elements.

  **Recurrence** (section [2.2.2.60](#Section_a033deda0745428db9cad08f3e6febcd))                     Specifies a collection of **Recurrence** elements that describe when and how often this meeting recurs.

  **Type** (section [2.2.2.80](#Section_df2f263ad570471883d080f2b5a2576e))                           Specifies the recurrence type of the recurring meeting.

  **Interval** (section [2.2.2.41](#Section_fe46604894324a86917498c1943c2a2f))                       Specifies the interval between recurrences of the recurring meeting.

  **Until** (section [2.2.2.85](#Section_d291b0fe6fb84b9f8a08a763f1f70d86))                          Specifies the end time of a series of recurrence meetings.

  **Occurrences** (section [2.2.2.55](#Section_7cb2ce2fa83642a8bebf82c94c699ed1))                    Specifies the number of occurrences before the series of recurring meeting ends.

  **WeekOfMonth** (section [2.2.2.88](#Section_08abd347d9734a1cbecbdde8679ebc78))                    Specifies the week of the month of the recurring meeting.

  **DayOfMonth** (section [2.2.2.25](#Section_87154586dfcb4452a0f98605f9c59212))                     Specifies the day of the month of the recurring meeting.

  **DayOfWeek** (section [2.2.2.26](#Section_9dc2f54241ef46598b0591ab96c8cb33))                      Specifies the day of the week of the recurring meeting.

  **MonthOfYear** (section [2.2.2.53](#Section_9a39d7f905a1407f9ddb93d0fd0eecd1))                    Specifies the month of the year of the recurring meeting.

  **email2:CalendarType** (section [2.2.2.15](#Section_fddb63ff4832450c9ef022239c4b9d37))            Specifies the type of calendar associated with the recurrence.

  **email2:IsLeapMonth** (section [2.2.2.43](#Section_a1d32fda006840cb9fc47f9d4124aa65))             Specifies whether the recurrence takes place in the leap month of the given year.

  **email2:FirstDayOfWeek** (section [2.2.2.33](#Section_415cfef0426541aca00ffcfbdef5d91b))          Specifies the day that is considered the first day of the calendar week for this recurrence.

  **Sensitivity** (section [2.2.2.71](#Section_4eae4b51efa24a5dad7ffc6916b3bf34))                    Specifies the confidentiality level of the meeting request.

  **BusyStatus** (section [2.2.2.14](#Section_728951b6615f47f5b073cd792c7ac51f))                     Specifies the intended busy status for the meeting request.

  **TimeZone** (section [2.2.2.78](#Section_a2108e339d484b3d80bf3818a77261a9))                       Specifies the time zone specified when the calendar item was created.

  **GlobalObjId** (section [2.2.2.37](#Section_9f00dbd2c0e5406c8d7452fffd0bbfd3))                    Specifies a hexadecimal ID generated by the client for the meeting request.

  **DisallowNewTimeProposal** (section [2.2.2.27](#Section_bac187fa43cb41838c3de0a594efad7c))        Specifies whether recipients can propose a new meeting time.

  **email2:MeetingMessageType** (section [2.2.2.47](#Section_861c5d27e5d64e1ea61a37c9e84f0ded))      Specifies the type of the meeting message.

  **InternetCPID** (section [2.2.2.40](#Section_5152748b0d054b9bafeba1bb83d73439))                   Specifies the original [**code page**](#gt_210637d9-9634-4652-a935-ded3cd434f38) ID from the MIME message.

  **Flag** (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16))                           Specifies the flag associated with the item, along with the item\'s current status.

  **Status** (section [2.2.2.74](#Section_20fba2e8af71412abb1b29c8d0d3fd2d))                         Specifies the current status of the flag.

  **FlagType** (section [2.2.2.35](#Section_157c6b73940144e9b9e0fbeadef817f6))                       Specifies the type of the flag.

  **tasks:DateCompleted** (section [2.2.2.23](#Section_c4ce6efa4d0040af9bbf1582186dcf59))            Specifies the date on which the flagged item was completed.

  **CompleteTime** (section [2.2.2.19](#Section_79799c317acb46fcb4c4fdbac218c383))                   Specifies the time at which the flagged item was marked as finished.

  **tasks:StartDate** (section [2.2.2.72](#Section_98e703d64d254727896d49f555465aba))                Specifies the start date of the flagged item.

  **tasks:DueDate** (section [2.2.2.31](#Section_cc1cc4674ecd4ef68ee8d0e52e9da8b3))                  Specifies the due date of the flagged item.

  **tasks:UtcStartDate** (section [2.2.2.87](#Section_d671fbb9aa284cec8a69089935c504a8))             Specifies the [**Coordinated Universal Time (UTC)**](#gt_f2369991-a884-4843-a8fa-1505b6d5ece7) value of the local **tasks:StartDate**.

  **tasks:UtcDueDate** (section [2.2.2.86](#Section_07e6f69fc7a444a1b0254ec85cd83a0d))               Specifies the UTC value of the local **tasks:DueDate**.

  **tasks:ReminderSet** (section [2.2.2.64](#Section_e19643a39288466eb691ef98ef60df1a))              Specifies whether a reminder has been set for this flagged item.

  **tasks:ReminderTime** (section [2.2.2.65](#Section_6e3300c9da1b4f9aad4e95f131fdb140))             Specifies the date and time that the reminder is scheduled to occur.

  **tasks:OrdinalDate** (section [2.2.2.56](#Section_be2830cb41ab470a89a288eeddc1c4a2))              Specifies the time at which the client set the flag.

  **tasks:SubOrdinalDate** (section [2.2.2.76](#Section_74a2a826f08f467e9164359d7cf6a3d1))           Specifies a string used to sort items.

  **tasks:Subject** (section [2.2.2.75.2](#Section_6163992744994a5484d7935a9b591f56))                Specifies the subject of the flag as it would appear in a task list.

  **airsyncbase:NativeBodyType** (section [2.2.2.54](#Section_57a0b904b37d485180b6e27a27891a5f))     Specifies the format in which the item is stored on the server.

  **ContentClass** (section [2.2.2.20](#Section_720b0b498718425eb43f64cb5c44a903))                   Specifies the content class of the data.

  **email2:UmCallerID** (section [2.2.2.83](#Section_033620824bc546ceb4ec8b07db28bb2b))              Specifies the callback telephone number of the person who called or left an electronic voice message.

  **email2:UmUserNotes** (section [2.2.2.84](#Section_27f62d9295ea4e2b9dd13d9604c3b963))             Specifies user notes related to an electronic voice message.

  **email2:ConversationId** (section [2.2.2.21](#Section_588e67a134184965aa3dfb3e7d5efdab))          Specifies a unique identifier for a conversation.

  **email2:ConversationIndex** (section [2.2.2.22](#Section_ee3ec0eb27504da6ab66819644b6ffba))       Specifies a set of dates and times used by clients to generate a conversation tree view.

  **email2:LastVerbExecuted** (section [2.2.2.44](#Section_73497e8eb427449f806c4908f0f0e3bc))        Specifies the last action, such as reply or forward, which was taken on the message so that the client can display the related icon.

  **email2:LastVerbExecutionTime** (section [2.2.2.45](#Section_c48ed3fad13f465f95121cdcdda26855))   Specifies the time when the **email2:LastVerbExecuted** was performed on the message.

  **email2:ReceivedAsBcc** (section [2.2.2.59](#Section_77d2e50f3d6c447a82bc94b8c0b81526))           Specifies whether the recipient was blind carbon copied on a message.

  **email2:Sender** (section [2.2.2.70](#Section_f5203e036019463ea3d438b41cf8c517))                  Specifies the user that actually sent the message when the message was not sent by the user identified by **From**.

  **Categories** (section [2.2.2.16](#Section_3b761feb2905427e822182ab60becfc6))                     Specifies the user-selected categories for this message.

  **Category** (section [2.2.2.17](#Section_7badc142ebc3460f9a19e6cf1e3c317e))                       Specifies the category for this e-mail item.

  **airsyncbase:BodyPart** (section [2.2.2.11](#Section_de616ed9d9b64913bef47aed6f4fd9a7))           Specifies the unique [**message part**](#gt_1f032bde-d2f7-4fc8-87d0-090964e7b5a5) of the requested message along with other meta-data properties.

  **email2:AccountId** (section [2.2.2.1](#Section_27c8156259d94d18a43eed365695ec36))                Specifies the ID of the account that received the message.

  **rm:RightsManagementLicense** (section [2.2.2.68](#Section_a0b56454340e44a5abf29aaf15f26fe9))     Specifies the rights management settings.
  -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

#### AccountId

The **email2:AccountId** element is an optional element that specifies a unique identifier for the account that received a message. It is defined as an element in the **Email2** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

The **email2:AccountId** element value SHOULD equal one of the **email2:AccountId** element values included in the **Settings** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.18), which lists the all aggregate accounts that the user subscribes to. In the event that the **email2:AccountId** element value does not equal one of the **email2:AccountId** element values included in the **Settings** command response, the client SHOULD handle the case without error, as the message was aggregated from an unknown account or an account that has been removed from the aggregate account list.

The **email2:AccountId** element is not included for e-mail messages that were sent to the primary account, as identified by the **PrimarySmtpAddress** element (\[MS-ASCMD\] section 2.2.3.138).

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

#### AllDayEvent

The **AllDayEvent** element is an optional child element of the **MeetingRequest** element (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)) that specifies whether the meeting request is for an all-day event. It is defined as an element in the **Email** namespace.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

If the value of this element is set to 1, the meeting request corresponds to an all-day event. If the value of this element is set to 0 (zero), the meeting request does not correspond to an all-day event.

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

#### Attachment

The **Attachment** element is a required element that represents an email attachment. It is defined in the **Email** namespace and is a child element of the **email:Attachments** element (section [2.2.2.4.2](#Section_0ffe420083144be29b0d1d3974ff50c6)).

This element is a **container** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2. It has the following child elements.

-   **AttName**, as specified in section [2.2.2.6](#Section_e6eea96a5f7f497386f3c9c23e0919d5)

-   **AttSize**, as specified in section [2.2.2.8](#Section_1d7f339188e14daab8943e0cc48ec32e)

-   **AttOid**, as specified in section [2.2.2.7](#Section_f6cf0deab3d849a79632b8674a4a5fe7)

-   **AttMethod**, as specified in section [2.2.2.5](#Section_ac7ae25bbe524891b148538ee894656d)

-   **DisplayName**, as specified in section [2.2.2.28](#Section_8b222a02052e4d42b53c0e41d6d8c891)

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

The **airsyncbase:Attachment** element ([\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.7) is used instead of the **email:Attachment** element with protocol versions 12.0, 12.1, 14.0, 14.1, 16.0, and 16.1.

#### Attachments

The **Attachments** element is defined in the **Email** namespace, as specified in section [2.2.2.4.2](#Section_0ffe420083144be29b0d1d3974ff50c6), for use by protocol version 2.5. It is defined in the **AirSyncBase** namespace, as specified in section [2.2.2.4.1](#Section_14e0cbaef1c6499ba64c9293c675ef1c), for use by protocol versions 12.0, 12.1, 14.0, 14.1, 16.0, and 16.1.

##### Attachments (AirSyncBase Namespace)

The **airsyncbase:Attachments** element is an optional element that contains a collection of attachments. It is defined as an element in the **AirSyncBase** namespace.

The **airsyncbase:Attachments** element is a **container** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2. It has one or more **airsyncbase:Attachment** elements ([\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.7). For more details about the **airsyncbase:Attachments** element, see \[MS-ASAIRS\] section 2.2.2.8.

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

The **email:Attachments** element (section [2.2.2.4.2](#Section_0ffe420083144be29b0d1d3974ff50c6)) is used instead of the **airsyncbase:Attachments** element with protocol version 2.5.

##### Attachments (Email Namespace)

The **Attachments** element is an optional element that contains a collection of attachments in a command response. It is defined as an element in the **Email** namespace. The **Attachments** element MUST NOT be present in a command request.

This element is a **container** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2. It has one or more **Attachment** elements (section [2.2.2.3](#Section_a021a304914546058897b6668e3459d7)).

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

The **airsyncbase:Attachments** element (section [2.2.2.4.1](#Section_14e0cbaef1c6499ba64c9293c675ef1c)) is used instead of the **email:Attachments** element with protocol versions 12.0, 12.1, 14.0, 14.1, 16.0, 16.1.

#### AttMethod

The **AttMethod** element specifies the method in which the attachment was attached. It is defined in the **Email** namespace and is a required child element of the **Attachment** element (section [2.2.2.3](#Section_a021a304914546058897b6668e3459d7)).

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The following table defines the valid values of the **AttMethod** element.

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
  2.5                                 Yes

  12.0                                

  12.1                                

  14.0                                

  14.1                                

  16.0                                

  16.1                                
  -----------------------------------------------------------------------

#### AttName

The **AttName** element specifies the location of the attachment file to be retrieved from the server. It is defined in the **Email** namespace and is a required child element of the **Attachment** element (section [2.2.2.3](#Section_a021a304914546058897b6668e3459d7)).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

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

#### AttOid

The **AttOid** element specifies the unique identifier of the attachment. The unique identifier allows the attachment to be referenced within the item to which the attachment belongs. This element is defined in the **Email** namespace and is an optional child element of the **Attachment** element (section [2.2.2.3](#Section_a021a304914546058897b6668e3459d7)).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

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

#### AttSize

The **AttSize** element is specifies the estimated size, in bytes, of the attachment file. It is defined in the **Email** namespace and is a required child element of the **Attachment** element (section [2.2.2.3](#Section_a021a304914546058897b6668e3459d7)).

The value of this element is an **integer** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

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

#### Bcc

The **email2:Bcc** element is an optional element that specifies the [**blind carbon copy (Bcc) recipients**](#gt_f5634b00-a1bf-4143-bb4f-9cd9dbad2bc0) of an email. It is defined as an element of the **Email2** namespace.

This element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

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

#### Body

The **Body** element is defined in the **Email** namespace, as specified in section [2.2.2.10.2](#Section_d836459d3c7842e4ada7c551fdd8bda6), for use by protocol version 2.5. It is defined in the **AirSyncBase** namespace, as specified in section [2.2.2.10.1](#Section_c2e6f8024947446fa0c6a7fb684a37cf), for use by protocol versions 12.0, 12.1, 14.0, 14.1, 16.0, and 16.1.

##### Body (AirSyncBase Namespace)

The **airsyncbase:Body** element is an optional **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that specifies details about the body of an e-mail.

When included in a **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21), a **Search** command response (\[MS-ASCMD\] section 2.2.1.16), or an **ItemOperations** command response (\[MS-ASCMD\] section 2.2.1.10), the **airsyncbase:Body** element can contain the following child elements:

-   **airsyncbase:Type** ([\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.41.1): This element is required.

-   **airsyncbase:EstimatedDataSize** (\[MS-ASAIRS\] section 2.2.2.23.2): This element is optional.

-   **airsyncbase:Truncated** (\[MS-ASAIRS\] section 2.2.2.39.1): This element is optional.

-   **airsyncbase:Data** (\[MS-ASAIRS\] section 2.2.2.20.1): This element is optional in **Sync** command responses and **Search** command responses. This element is optional in **ItemOperations** command responses and is only included if a nonzero **airsyncbase:TruncationSize** (\[MS-ASAIRS\] section 2.2.2.40.2) element value was included in the request and the **airsyncbase:AllOrNone** (\[MS-ASAIRS\] section 2.2.2.3.2) element value included in the request does not restrict content from being returned in the response.

The **airsyncbase:Body** element is defined as an element in the **AirSyncBase** namespace, and is further specified in \[MS-ASAIRS\] section 2.2.2.9.

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

The **email:Body** element (section [2.2.2.10.2](#Section_d836459d3c7842e4ada7c551fdd8bda6)) is used instead of the **airsyncbase:Body** element with protocol version 2.5.

##### Body (Email Namespace)

The **Body** element is an optional element that contains the body of an email message that is retrieved from the server. This element is defined in the **Email** namespace as a child of the **airsync:ApplicationData** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.11) in **Sync** command requests and responses (\[MS-ASCMD\] section 2.2.1.21).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

A client can use the **airsync:Truncation** element, as specified in \[MS-ASCMD\] section 2.2.3.185, to request truncation of a message body. This conserves space and reduces data traffic when synchronizing email messages. The server sets the **BodyTruncated** element (section [2.2.2.13](#Section_e327e10687644cabb78863d8f3fc257c)) in the **Sync** response to indicate whether the body of the email message has actually been truncated. The untruncated size of the message body is specified by the **BodySize** element (section [2.2.2.12](#Section_39851d9eaa364751a43d2953fe2b6da1)).

When the client requests truncation, only the first part (or none) of each message body is included in a synchronization. The client can later request the full message body from the server. The client\'s user interface generally lets the user view the first part of the email message and choose to download the rest, either immediately or on the next synchronization.

If the client enables [**MIME**](#gt_af6ba277-34c1-493d-8103-71d2af36ce30) support by setting the **airsync:MIMESupport** element, as specified in \[MS-ASCMD\] section 2.2.3.110.3, the server sends MIME data for some or all email messages, depending on the level of support indicated by the value of the **airsync:MIMESupport** element. The server uses the **MIMEData**, **MIMESize**, and **MIMETruncated** elements (sections [2.2.2.50](#Section_6d4ca3d1b5424ee189633867089468fb), [2.2.2.51](#Section_2275c29d592a4ddc97cd336415165eec), and [2.2.2.52](#Section_ad30e1a89e93424fbda03a473074983b), respectively) instead of the **Body**, **BodySize**, and **BodyTruncated** elements when sending MIME data for email messages.

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

The **airsyncbase:Body** element (section [2.2.2.10.1](#Section_c2e6f8024947446fa0c6a7fb684a37cf)) is used instead of the **email:Body** element with protocol versions 12.0, 12.1, 14.0, 14.1, 16.0, and 16.1.

#### BodyPart

The **airsyncbase:BodyPart** element is an optional **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that specifies details about the message part of an e-mail message. It is defined as an element in the **AirSyncBase** namespace.

For more details about the **airsyncbase:BodyPart** element and its child elements, see [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.10.

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

#### BodySize

The **BodySize** element is an optional element that specifies the full size, in characters, of the email message body. This element is defined in the **Email** namespace as a child of the **airsync:ApplicationData** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.11) in **Sync** command responses (\[MS-ASCMD\] section 2.2.1.21).

The value of this element is an **integer** data type, as specified in as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

This element is present only when the **BodyTruncated** element (section [2.2.2.13](#Section_e327e10687644cabb78863d8f3fc257c)) is set to 1. When the message body is truncated, the **BodySize** element is included to specify the original size of the message body prior to truncation.

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

The **BodyTruncated** element is an optional element that indicates whether the body of the email message was truncated when sent from the server. This element is defined in the **Email** namespace as a child of the **airsync:ApplicationData** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.11) in **Sync** command responses (\[MS-ASCMD\] section 2.2.1.21).

The value of this element is a **boolean** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.1.

A value of 1 indicates that the message body has been truncated by the server; a value of 0 (zero) indicates that the message body has not been truncated.

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

The **BusyStatus** element is an optional child element of the **MeetingRequest** element (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)) that specifies the busy status of the [**recipient**](#gt_53dfe4f3-05d0-41aa-8217-ecd1962b340b) for the meeting, once the meeting request is accepted. It is defined as an element in the **Email** namespace.

The value of this element is an **integer** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

The value of this element MUST be one of the values listed in the following table.

  -----------------------------------------------------------------------------------------
  Value               Meaning
  ------------------- ---------------------------------------------------------------------
  0                   Free

  1                   [**Tentative**](#gt_78bfb817-fde0-4756-9cae-7c68c5c962f5)

  2                   Busy

  3                   [**Out of Office (OOF)**](#gt_d4ab6719-b583-467a-a631-95feb7a5ea34)

  4                   Working Elsewhere
  -----------------------------------------------------------------------------------------

If this element is not present, a default value of 2 MUST be assumed.

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

The value 4 (Working Elsewhere) is not supported in protocol versions 2.5, 12.0, 12.1, 14.0 and 14.1.

#### CalendarType

The **email2:CalendarType** element is a child element of the **Recurrence** element (section [2.2.2.60](#Section_a033deda0745428db9cad08f3e6febcd)) that specifies the type of calendar associated with the recurrence. This element is required when the **Type** element (section [2.2.2.80](#Section_df2f263ad570471883d080f2b5a2576e)) value is 2, 3, 5, or 6 in server responses. It is defined as an element in the **Email2** namespace.

The value of this element is an **integer** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

The following table lists valid values for the **email2:CalendarType** element.

  -----------------------------------------------------------------------
  Value            Meaning
  ---------------- ------------------------------------------------------
  0                Default

  1                Gregorian

  2                Gregorian US

  3                Japan

  4                Taiwan

  5                Korea

  6                Hijri

  7                Thai

  8                Hebrew

  9                GregorianMeFrench

  10               Gregorian Arabic

  11               Gregorian translated English

  12               Gregorian translated French

  14               Japanese Lunar

  15               Chinese Lunar

  20               Korean Lunar
  -----------------------------------------------------------------------

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

The **Categories** element is an optional **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that specifies a collection of user-selected categories assigned to the e-mail message. It is defined as an element in the **Email** namespace.

The **Categories** element can contain the following child element:

-   **Category** (section [2.2.2.17](#Section_7badc142ebc3460f9a19e6cf1e3c317e)): This element is optional.

An empty **Categories** element is included as a child of the **Add** ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.7.2) element in a **Sync** (\[MS-ASCMD\] section 2.2.1.21) command if no child **Category** elements have been set on the message. A **Sync** command that contains a **Change** element (\[MS-ASCMD\] section 2.2.3.24) that contains an empty **Categories** element indicates that all categories associated with the message have been removed. A **Sync** command that contains a **Change** element that does not contain a **Categories** element indicates that the categories on the message have not changed.

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

#### Category

The **Category** element is an optional child element of the **Categories** element (section [2.2.2.16](#Section_3b761feb2905427e822182ab60becfc6)) that specifies a category that is assigned to the e-mail item. It is defined as an element in the **Email** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

A command request SHOULD include no more than 300 **Category** child elements per **Categories** element.

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

#### Cc

The **Cc** element is an optional element that specifies the list of secondary [**recipients**](#gt_53dfe4f3-05d0-41aa-8217-ecd1962b340b) of a message. It is defined as an element in the **Email** namespace.

The message is directed at the primary recipient as specified by the **To** element (section [2.2.2.79](#Section_86168c1043494946a8bb9ce39fd9bb24)), but the secondary recipients also receive a copy of the message.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7. The value of this element contains one or more e-mail addresses. If there are multiple e-mail addresses, they are separated by commas.

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
  -----------------------------------------------------------------------

#### CompleteTime

The **CompleteTime** element is an optional child element of the **Flag** element (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16)) that identifies the time at which a flagged item was marked as finished. It is defined as an element in the **Email** namespace.

The **CompleteTime** element is required to mark a flagged item as complete. If a message includes a value for the **CompleteTime** element, the message SHOULD also include a value for the **tasks:DateCompleted** element. The value of this element is a **dateTime** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.3.

A maximum of one **CompleteTime** child element is allowed per **Flag** element.

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

#### ContentClass

The **ContentClass** element is an optional element that specifies the content class of the data. It is defined as an element in the **Email** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

For e-mail messages, the value of this element MUST be set to \"urn:content-classes:message\".

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

#### ConversationId

The **email2:ConversationId** element is a required element in server responses that specifies a unique identifier for a conversation. It is defined as an element in the **Email2** namespace.

The value of this element is a **byte array** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.1.

The client MUST NOT define or change the value of the **email2:ConversationId** element. The server returns a **Status** element value of 6 in the **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) when the **email2:ConversationId** element is included within a **Change** element (\[MS-ASCMD\] section 2.2.3.24) in a **Sync** command request.

The **email2:ConversationId** content is transferred as an opaque [**binary large object (BLOB)**](#gt_ad861812-8cb0-497a-80bb-13c95aa4e425) within the [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc) tags.

For more details about conversations, see [\[MS-ASCON\]](%5bMS-ASCON%5d.pdf#Section_8571bf985f7b4c2fab28c32176d20169).

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

In protocol versions 16.0 and 16.1: When the client adds a new draft item, the server response will contain the **email2:ConversationId** element for that draft item.

#### ConversationIndex

The **email2:ConversationIndex** element is a required element in server responses. This element contains a set of timestamps used by clients to generate a conversation tree view. The first timestamp identifies the date and time when the message was originally sent by the server. Additional timestamps are added when the message is forwarded or replied to. The **email2:ConversationIndex** element is defined as an element in the **Email2** namespace.

The value of this element is a **byte array** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.1.

The **email2:ConversationIndex** content is transferred as an opaque [**binary large object (BLOB)**](#gt_ad861812-8cb0-497a-80bb-13c95aa4e425) within the [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc) tags.

For more details about the **email2:ConversationIndex** element, see [\[MS-ASCON\]](%5bMS-ASCON%5d.pdf#Section_8571bf985f7b4c2fab28c32176d20169) section 2.2.2.4.

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

In protocol versions 16.0 and 16.1: When the client adds a new draft item, the server response will include the **email2:ConversationIndex** element for that draft item.

#### DateCompleted

The **tasks:DateCompleted** element is an optional child element of the **Flag** element (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16)) that identifies the date on which a flagged item was completed. It is defined as an element in the **Tasks** namespace.

The **tasks:DateCompleted** element is required to mark a flagged item as complete. If a message includes a value for the **tasks:DateCompleted** element, the **CompleteTime** element (section [2.2.2.19](#Section_79799c317acb46fcb4c4fdbac218c383)) is also required. The value of this element is a **dateTime** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.3.

A maximum of one **tasks:DateCompleted** child element is allowed per **Flag** element.

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

#### DateReceived

The **DateReceived** element is an optional element that specifies the date and time the message was received by the current [**recipient**](#gt_53dfe4f3-05d0-41aa-8217-ecd1962b340b). It is defined as an element in the **Email** namespace. The value of this element is a **dateTime** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.3.

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

#### DayOfMonth

The **DayOfMonth** element is an optional child element of the **Recurrence** element (section [2.2.2.60](#Section_a033deda0745428db9cad08f3e6febcd)) that specifies the day of the month on which the meeting recurs. It is defined as an element in the **Email** namespace.

The value of this element is an **integer** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

This element is required when the **Type** element (section [2.2.2.80](#Section_df2f263ad570471883d080f2b5a2576e)) is set to a value of 2 (that is, the meeting recurs monthly on the Nth day of the month), or a value of 5 (that is, the meeting recurs yearly on the Nth day of the Nth month each year).

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

The **DayOfWeek** element is an optional child element of the **Recurrence** element (section [2.2.2.60](#Section_a033deda0745428db9cad08f3e6febcd)) that specifies the day of the week on which this meeting recurs. It is defined as an element in the **Email** namespace.

The value of this element is an **integer** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

This element is required when the **Type** element (section [2.2.2.80](#Section_df2f263ad570471883d080f2b5a2576e)) is set to a value of 1 (that is, the meeting recurs weekly), or a value of 6 (that is, the meeting recurs yearly on the Nth day of the week of the Nth month each year).

The value of this element MUST be the sum of a minimum of one and a maximum of seven independent values from the following table.

  -----------------------------------------------------------------------
  Value                       Meaning
  --------------------------- -------------------------------------------
  1                           Sunday

  2                           Monday

  4                           Tuesday

  8                           Wednesday

  16                          Thursday

  32                          Friday

  64                          Saturday
  -----------------------------------------------------------------------

These values can be added together to specify that the meeting occurs on more than one day of the week.

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

The **DisallowNewTimeProposal** element is an optional child element of the **MeetingRequest** element (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)) that indicates whether [**recipients**](#gt_53dfe4f3-05d0-41aa-8217-ecd1962b340b) can propose a new meeting time for the meeting. It is defined as an element in the **Email** namespace.

If this element is not specified, the value defaults to 0 (zero), meaning that new time proposals are allowed. A nonzero value indicates that new time proposals are not allowed.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

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

#### DisplayName

The **DisplayName** element specifies the name of the attachment file as displayed to the user. This element is defined in the **Email** namespace and is an optional child element of the **Attachment** element (section [2.2.2.3](#Section_a021a304914546058897b6668e3459d7)).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

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

#### DisplayTo

The **DisplayTo** element is an optional element that specifies the e-mail addresses of the primary [**recipients**](#gt_53dfe4f3-05d0-41aa-8217-ecd1962b340b) of this message. It is defined as an element in the **Email** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7. The value of this element contains one or more display names. If there are multiple display names, they are separated by semi-colons.

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

#### DtStamp

The **DtStamp** element is a required child element of the **MeetingRequest** element (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)) that specifies the date and time the calendar item was created. It is defined as an element in the **Email** namespace.

The value of this element is a **dateTime** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.3.

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

#### DueDate

The **tasks:DueDate** element is an optional child element of the **Flag** element (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16)) that specifies when the flagged item is due. It is defined as an element in the **Tasks** namespace.

When a flag is updated, the **tasks:DueDate** element value MUST NOT occur before the **tasks:StartDate** element (section [2.2.2.72](#Section_98e703d64d254727896d49f555465aba)) value. The server returns a **Status** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.177.17) value of 6 in the **Sync** command response (\[MS-ASCMD\] section 2.2.1.21) if this condition is not met.

To set a flag, the **tasks:StartDate** element, **tasks:DueDate** element, **tasks:UtcStartDate** element (section [2.2.2.87](#Section_d671fbb9aa284cec8a69089935c504a8)), and **tasks:UtcDueDate** element (section [2.2.2.86](#Section_07e6f69fc7a444a1b0254ec85cd83a0d)) either all MUST be set, or all MUST be NULL. The server returns a **Status** element value of 6 in the **Sync** response if this condition is not met.

The value of this element is a **dateTime** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.3.

A maximum of one **tasks:DueDate** child element is allowed per **Flag** element. The result of including more than one **tasks:DueDate** child element per **Flag** element is undefined. The server MAY return a protocol status error in response to such a command request.

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

#### EndTime

The **EndTime** element is an optional child element of the **MeetingRequest** element (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)) that specifies the date and time when the meeting ends. It is defined as an element in the **Email** namespace.

The value of this element is a **dateTime** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.3.

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

#### FirstDayOfWeek

The **email2:FirstDayOfWeek** element is an optional child element of the **Recurrence** element (section [2.2.2.60](#Section_a033deda0745428db9cad08f3e6febcd)) that specifies which day is considered the first day of the calendar week for the recurrence. It is defined as an element in the **Email2** namespace.

A command request has a maximum of one **email2:FirstDayOfWeek** child element per **Recurrence** element. A command response has a maximum of one **email2:FirstDayOfWeek** child element per **Recurrence** element.

This element disambiguates recurrences when attendees live across localities that define a different starting day for the calendar week. If this element is not included in the client request, the server SHOULD identify the first day of the week for this recurrence according to the preconfigured options of the user creating the calendar item.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The value of the **email2:FirstDayOfWeek** element MUST be one of the values listed in the following table.

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

#### Flag

The **Flag** element is an optional **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that defines the flag associated with the item and indicates the item\'s current status. It is defined as an element in the **Email** namespace.

If flags are present on the e-mail item, the **Flag** element contains one or more child elements that define the flag. If no flags are present on the e-mail item, the **Flag** element SHOULD be included in the command as an empty container element (that is, \"\<Flag/\>\").

The **Flag** element can contain the following child elements:

-   **tasks:Subject** (section [2.2.2.75.2](#Section_6163992744994a5484d7935a9b591f56)): This element is optional.

-   **Status** (section [2.2.2.74](#Section_20fba2e8af71412abb1b29c8d0d3fd2d)): This element is optional.

-   **FlagType** (section [2.2.2.35](#Section_157c6b73940144e9b9e0fbeadef817f6)): This element is optional.

-   **tasks:DateCompleted** (section [2.2.2.23](#Section_c4ce6efa4d0040af9bbf1582186dcf59)): This element is optional.

-   **CompleteTime** (section [2.2.2.19](#Section_79799c317acb46fcb4c4fdbac218c383)): This element is optional.

-   **tasks:StartDate** (section [2.2.2.72](#Section_98e703d64d254727896d49f555465aba)): This element is optional.

-   **tasks:DueDate** (section [2.2.2.31](#Section_cc1cc4674ecd4ef68ee8d0e52e9da8b3)): This element is optional.

-   **tasks:UtcStartDate** (section [2.2.2.87](#Section_d671fbb9aa284cec8a69089935c504a8)): This element is optional.

-   **tasks:UtcDueDate** (section [2.2.2.86](#Section_07e6f69fc7a444a1b0254ec85cd83a0d)): This element is optional.

-   **tasks:ReminderSet** (section [2.2.2.64](#Section_e19643a39288466eb691ef98ef60df1a)): This element is optional.

-   **tasks:ReminderTime** (section [2.2.2.65](#Section_6e3300c9da1b4f9aad4e95f131fdb140)): This element is optional.

-   **tasks:OrdinalDate** (section [2.2.2.56](#Section_be2830cb41ab470a89a288eeddc1c4a2)): This element is optional.

-   **tasks:SubOrdinalDate** (section [2.2.2.76](#Section_74a2a826f08f467e9164359d7cf6a3d1)): This element is optional.

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

#### FlagType

The **FlagType** element is an optional child element of the **Flag** element (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16)) that specifies the type of the flag. It is defined as an element in the **Email** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

The **FlagType** element is not required if the e-mail message is a meeting request or response.

This element value is customizable, and is commonly set to \"Flag for follow up\" or \"for Follow Up\".

A maximum of one **FlagType** child element is allowed per **Flag**.

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

#### From

The **From** element is an optional element that specifies the e-mail address of the message sender. It is defined as an element in the **Email** namespace.

The value of the **From** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7, and has a maximum length of 32,768 characters.

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

#### GlobalObjId

The **GlobalObjId** element is a required child element of the **MeetingRequest** element (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)) that contains a hexadecimal ID generated by the server for the meeting request. It is defined as an element in the **Email** namespace.

The following [**Augmented Backus-Naur Form (ABNF)**](#gt_24ddbbb4-b79e-4419-96ec-0fdd229c9ebf) notation specifies the format of the **GlobalObjId** element.

1.  GLOBALOBJID = CLASSID INSTDATE NOW RESERVED BYTECOUNT DATA

    CLASSID = %x04 %x00 %x00 %x00 %x82 %x00 %xE0 %x00 %x74 %xC5 %xB7 %x10 %x1A %x82 %xE0 %x08

    INSTDATE = (%x00 %x00 %x00 %x00) \| (YEARHIGH YEARLOW MONTH DATE)

    ; The high order byte of the year. For example, the year 2004 would be 0x07.

    YEARHIGH = BYTE

    ; The low order byte of year. For example, the year 2004 would be 0xD4.

    YEARLOW = BYTE

    ; The month of the specific instance.

    MONTH = %x01-12

    ; The date of the specific instance.\
    DATE = %x01-31

    ; The current date expressed as number 100 nanosecond intervals since 1/1/1601 in little-endian byte order.

    NOW = 4BYTE 4BYTE

    ; Reserved bytes.

    RESERVED = 8BYTE

    ; The length of following data in little-endian byte order.\
    BYTECOUNT = 4BYTE

    DATA = OUTLOOKID \| VCALID

    ; The length specified by BYTECOUNT.\
    OUTLOOKID = \*BYTE

    VCALID = VCALSTRING VERSION UID %x00\
    \
    ; A marker indicating that the identifier is a vCal identifier.

    VCALSTRING = \"vCal-Uid\"

    VERSION = %x01 %x00 %x00 %x00\
    \
    ; The length is BYTECOUNT less the length of VCALSTRING less the length of VERSION ; less 1 byte for \<00\>.

    UID = \*BYTE

    BYTE = %x00-FF

    NULL = %x00

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

The server will return the **calendar:UID** element ([\[MS-ASCAL\]](%5bMS-ASCAL%5d.pdf#Section_0c4486824a6a459aae662fed0712bef9) section 2.2.2.46) instead of the **GlobalObjId** element when protocol version 16.0 or 16.1 is used.

#### Importance

The **Importance** element is an optional element that specifies the importance of the message, as assigned by the sender. It is defined as an element in the **Email** namespace.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The value of this element MUST be one of the values listed in the following table.

  -----------------------------------------------------------------------
  Value                   Meaning
  ----------------------- -----------------------------------------------
  0 (zero)                Low importance

  1                       Normal importance

  2                       High importance
  -----------------------------------------------------------------------

If this element is omitted from a command response, then clients MUST assume a value of 1 as the default value.

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

#### InstanceType

The **InstanceType** element is an optional child element of the **MeetingRequest** element (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)) that specifies whether the calendar item is a single or recurring appointment. It is defined as an element in the **Email** namespace.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The value of this element MUST be one of the values listed in the following table.

  -----------------------------------------------------------------------------------------------------------
  Value      Meaning
  ---------- ------------------------------------------------------------------------------------------------
  0          A single appointment.

  1          A master recurring appointment.

  2          A single instance of a recurring appointment.

  3          An exception to a recurring appointment.

  4          An [**orphan instance**](#gt_0efee4a8-a2e9-48fe-87f8-d45097de6b72) of a recurring appointment.
  -----------------------------------------------------------------------------------------------------------

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

The value 4 is not supported by protocol versions 2.5, 12.0, 12.1, 14.0 and 14.1.

#### InternetCPID

The **InternetCPID** element is a required element that contains the original [**code page**](#gt_210637d9-9634-4652-a935-ded3cd434f38) ID from the [**MIME**](#gt_af6ba277-34c1-493d-8103-71d2af36ce30) message. It is defined as an element in the **Email** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

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

#### Interval

The **Interval** element is a required child element of the **Recurrence** element (section [2.2.2.60](#Section_a033deda0745428db9cad08f3e6febcd)) that specifies the interval between meeting recurrences. It is defined as an element in the **Email** namespace.

The value of this element is an **integer** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

An **Interval** element value of 1 indicates that the meeting occurs every week, month, or year, depending upon the value of the **Type** element (section [2.2.2.80](#Section_df2f263ad570471883d080f2b5a2576e)). An **Interval** element value of 2 indicates that the meeting occurs every other week, month, or year.

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

#### IsDraft

The **email2:IsDraft** element is an optional element that specifies whether an email is a draft. It is defined as an element of the **Email2** namespace.

Clients MUST NOT include this element in a command request.

This element is a **boolean** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.1. The value 1 (TRUE) indicates that the email is a draft; the value 0 (FALSE) indicates that the email is not a draft.

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

#### IsLeapMonth

The **email2:IsLeapMonth** element is an optional child element of the **Recurrence** element (section [2.2.2.60](#Section_a033deda0745428db9cad08f3e6febcd)) that specifies whether the recurrence takes place in the leap month of the given year. It is defined as an element in the **Email2** namespace.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

This element is required in server responses and is optional in client requests. A default value of 0 (zero, meaning FALSE) is used if the element value is not specified in the client request.

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

#### LastVerbExecuted

The **email2:LastVerbExecuted** element is an optional element that indicates the last action, such as reply or forward, that was taken on the message. It is defined as an element in the **Email2** namespace. The client SHOULD use the value of this element to display the icon related to the message.

The value of this element is an **integer** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

The following table lists the valid values for this element.

  -----------------------------------------------------------------------
  Value                  Meaning
  ---------------------- ------------------------------------------------
  0                      Unknown

  1                      REPLYTOSENDER

  2                      REPLYTOALL

  3                      FORWARD
  -----------------------------------------------------------------------

The value of this element, together with the value of the **LastVerbExecutionTime** element (section [2.2.2.45](#Section_c48ed3fad13f465f95121cdcdda26855)), represents the reply state of the message.

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

#### LastVerbExecutionTime

The **email2:LastVerbExecutionTime** element is an optional element that indicates the date and time when the action specified by the **email2:LastVerbExecuted** element (section [2.2.2.44](#Section_73497e8eb427449f806c4908f0f0e3bc)) was performed on the message. It is defined as an element in the **Email2** namespace.

The value of this element is a **dateTime** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.3.

The value of the **LastVerbExecuted** element (section 2.2.2.44), together with the value of this element, represents the reply state of the message.

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

The **Location** element is an optional child element of the **MeetingRequest** element (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)) that specifies where the meeting will occur. It is defined as an element in the **Email** namespace.

The value of the **Location** element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7, and has a maximum length of 32,768 characters.

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

The **airsyncbase:Location** element ([\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.28) is used instead of the **email:Location** element in protocol versions 16.0 and 16.1.

#### MeetingMessageType

The **email2:MeetingMessageType** element is a required child element of the **MeetingRequest** element (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)) that specifies the type of meeting message. It is defined as an element in the **Email2** namespace.

The value of this element is an **unsignedByte** value, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The **email2:MeetingMessageType** value is not change tracked within e-mail messages, and therefore is not updated if the value is changed after the meeting request is sent to the client.

The value of this element MUST be one of the values listed in the following table.

  -----------------------------------------------------------------------------------------------------------------------------
  Value   Meaning
  ------- ---------------------------------------------------------------------------------------------------------------------
  0       A silent update was performed, or the message type is unspecified.

  1       Initial meeting request.

  2       Full update.

  3       Informational update.

  4       Outdated. A newer meeting request or meeting update was received after this message.

  5       Identifies the delegator\'s copy of the meeting request.

  6       Identifies that the meeting request has been delegated and the meeting request MUST NOT be responded to.[\<1\>](\l)
  -----------------------------------------------------------------------------------------------------------------------------

If this element is missing, then a default of 0 (zero) MUST be assumed.

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

#### MeetingRequest

The **MeetingRequest** element is an optional **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that contains information about the meeting. It is defined as an element in the **Email** namespace.

The **MeetingRequest** element is included as message data when the e-mail message is a meeting request and the client\'s user is an attendee. The **MeetingRequest** element is not included as message data of either normal e-mail messages or calendar items in the [**Calendar folder**](#gt_60b55610-ca65-41f2-91d8-a4d6f4cc6d20). If a message contains the **MeetingRequest** element, the client can respond to the meeting request by using the **MeetingResponse** command, as specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.11.

The **MeetingRequest** element can contain the following child elements in a command response:

-   **AllDayEvent** (section [2.2.2.2](#Section_4ff47c4583b147189114b668be141793)): This element is optional.

-   **StartTime** (section [2.2.2.73](#Section_d05462bb7b18432a8cc132179ad2e0fb)): This element is optional.

-   **DtStamp** (section [2.2.2.30](#Section_6ec89d167c0546d080d20801a28d9157)): One instance of this element is required.

-   **EndTime** (section [2.2.2.32](#Section_41b811f7a98946d0abc158706c150623)): This element is optional.

-   **InstanceType** (section [2.2.2.39](#Section_a4d4823969364b229267623ecdb49b5e)): One instance of this element is optional.

-   **Location** (section [2.2.2.46](#Section_1356b4a405654bda815663165ed1e503)): This element is optional. See the details about protocol versions at the end of this section.

-   **airsyncbase:Location** ([\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.28): This element is optional. See the details about protocol versions at the end of this section.

-   **Organizer** (section [2.2.2.57](#Section_d17abe24f69c46d8b7fabf29ee6fb63f)): This element is optional.

-   **RecurrenceId** (section [2.2.2.61](#Section_262bccb1697e4d2c8146227005bcb4b9)): This element is optional.

-   **Reminder** (section [2.2.2.63](#Section_8ba415190ee844adbd662a9541f365ae)): This element is optional.

-   **ResponseRequested** (section [2.2.2.67](#Section_eccb418ee5964ce9a5fa138ffe8aa14a)): This element is optional.

-   **Recurrences** (section [2.2.2.62](#Section_8722b485daac45aea64035241de8233c)): This element is optional.

-   **Sensitivity** (section [2.2.2.71](#Section_4eae4b51efa24a5dad7ffc6916b3bf34)): This element is optional.

-   **BusyStatus** (section [2.2.2.14](#Section_728951b6615f47f5b073cd792c7ac51f)): This element is optional.

-   **TimeZone** (section [2.2.2.78](#Section_a2108e339d484b3d80bf3818a77261a9)): One instance of this element is required.

-   **GlobalObjId** (section [2.2.2.37](#Section_9f00dbd2c0e5406c8d7452fffd0bbfd3)): One instance of this element is required. See the details about protocol versions at the end of this section.

-   **calendar:UID** ([\[MS-ASCAL\]](%5bMS-ASCAL%5d.pdf#Section_0c4486824a6a459aae662fed0712bef9) section 2.2.2.46): One instance of this element is required. See the details about protocol versions at the end of this section.

-   **DisallowNewTimeProposal** (section [2.2.2.27](#Section_bac187fa43cb41838c3de0a594efad7c)): This element is optional.

-   **MeetingMessageType** (section [2.2.2.47](#Section_861c5d27e5d64e1ea61a37c9e84f0ded)): This element is required.

-   **ProposedStartTime** (\[MS-ASCMD\] section 2.2.3.141): This element is optional.

-   **ProposedEndTime** (\[MS-ASCMD\] section 2.2.3.140): This element is optional.

-   **Forwardees** (\[MS-ASCMD\] section 2.2.3.79): This element is optional.

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

In protocol versions 16.0 and 16.1, the **calendar:UID** element is used instead of the **email:GlobalObjId** element, and the **airsyncbase:Location** element is used instead of the **email:Location** element.

#### MessageClass

The **MessageClass** element is an optional element that specifies the message class of this e-mail message. It is defined as an element in the **Email** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

The **MessageClass** element value provides a hint that the client SHOULD use to aid in processing the item. This protocol does not validate that the item has the correct **MessageClass** element value, nor does it update incorrect values.

The value of the **MessageClass** element SHOULD be one of the values listed in the following table or derive from one of the values listed in the following table. This protocol supports the following message classes as well as all subclasses of the same namespaces. The values are case insensitive.

  -------------------------------------------------------------------------------------------------------------------------------------------------
  Value                            Meaning
  -------------------------------- ----------------------------------------------------------------------------------------------------------------
  IPM.Note                         Normal e-mail message.

  IPM.Note.SMIME                   The message is encrypted and can also be signed.

  IPM.Note.SMIME.MultipartSigned   The message is clear signed.

  IPM.Note.Receipt.SMIME           The message is a secure [**read receipt**](#gt_482683b0-5cf4-483f-b41e-162383fbb5ca).

  IPM.InfoPathForm                 An InfoPath form, as specified by [\[MS-IPFFX\]](%5bMS-IPFFX%5d.pdf#Section_18d25c38f26448e0b64dc71ce00b2de4).

  IPM.Schedule.Meeting             Meeting request.

  IPM.Notification.Meeting         Meeting notification.

  IPM.Post                         Post.

  IPM.Octel.Voice                  Octel voice message.

  IPM.Voicenotes                   Electronic voice notes.

  IPM.Sharing                      Shared message.
  -------------------------------------------------------------------------------------------------------------------------------------------------

In addition, certain administrative messages, such as read receipts and non-delivery reports that are generated by the server, have a message class that is derived from one of the message classes listed in the preceding table. The format of this value is a prefix of \"REPORT\" and a suffix that indicates the type of report. For these administrative messages, the value of the **MessageClass** element MUST be one of the following values. The values are case insensitive.

  -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Value                                         Meaning
  --------------------------------------------- -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  REPORT.IPM.NOTE.NDR                           [**Non-delivery report**](#gt_2540c3dc-aeea-4d46-bf5a-a019d9e645f5) for a standard message.

  REPORT.IPM.NOTE.DR                            [**Delivery receipt**](#gt_9d58a6d9-25fe-4093-98bd-f5838ac51a47) for a standard message.

  REPORT.IPM.NOTE.DELAYED                       Delivery receipt for a delayed message.

  \*REPORT.IPM.NOTE.IPNRN                       Read receipt for a standard message.

  \*REPORT.IPM.NOTE.IPNNRN                      [**Non-read receipt**](#gt_82b4c00f-7f31-46d6-90e1-459aaf901bd6) for a standard message.

  REPORT.IPM.SCHEDULE. MEETING.REQUEST.NDR      Non-delivery report for a meeting request.

  REPORT.IPM.SCHEDULE.MEETING.RESP.POS.NDR      Non-delivery report for a positive meeting response (accept).

  REPORT.IPM.SCHEDULE.MEETING.RESP.TENT.NDR     Non-delivery report for a [**Tentative**](#gt_78bfb817-fde0-4756-9cae-7c68c5c962f5) meeting response.

  REPORT.IPM.SCHEDULE.MEETING.CANCELED.NDR      Non-delivery report for a cancelled meeting notification.

  REPORT.IPM.NOTE.SMIME.NDR                     Non-delivery report for a Secure MIME ([**S/MIME**](#gt_84bfada5-a327-4110-a257-cffd8fc3fe61)) encrypted and [**opaque-signed message**](#gt_7be29e6d-10e1-4658-8735-1c4f01f77d1b).

  \*REPORT.IPM.NOTE.SMIME.DR                    Delivery receipt for an S/MIME encrypted and opaque-signed message.

  \*REPORT.IPM.NOTE.SMIME.MULTIPARTSIGNED.NDR   Non-delivery report for an S/MIME [**clear-signed message**](#gt_a805ae0b-0a4e-43ae-b75d-de65a36fa73c).

  \*REPORT.IPM.NOTE.SMIME.MULTIPARTSIGNED.DR    Delivery receipt for an S/MIME clear-signed message.
  -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

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

#### MIMEData

The **MIMEData** element is an optional element that contains the raw [**MIME**](#gt_af6ba277-34c1-493d-8103-71d2af36ce30) data of an email message that is retrieved from the server. This element is defined in the **Email** namespace as a child of the **airsync:ApplicationData** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.11) in **Sync** command responses (\[MS-ASCMD\] section 2.2.1.21).

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

This element is returned by the server only if the client enables MIME support by setting the **airsync:MIMESupport** element, as specified in \[MS-ASCMD\] section 2.2.3.110.3, to a nonzero value. If the client has not enabled MIME support, the server returns the email message body in the **Body** element (section [2.2.2.10.2](#Section_d836459d3c7842e4ada7c551fdd8bda6)).

If the size, in characters, of the MIME data exceeds the value specified by the client in the **airsync:MIMETruncation** element (\[MS-ASCMD\] section 2.2.3.111), the string returned in the **MIMEData** element will be truncated up to the value specified in the **airsync:MIMETruncation** element. When the server truncates the MIME data, the value of the **MIMESize** element (section [2.2.2.51](#Section_2275c29d592a4ddc97cd336415165eec)) contains the original size, in characters, of the MIME data prior to truncation. The server sets the **MIMETruncated** element (section [2.2.2.52](#Section_ad30e1a89e93424fbda03a473074983b)) to 1 to indicate that the string contained in the **MIMEData** element has been truncated. When the client uses the **airsync:Fetch** element (\[MS-ASCMD\] section 2.2.3.67.2), the complete MIME data of the email message will be returned to the client regardless of the value of the **airsync:MIMETruncation** element.

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

#### MIMESize

The **MIMESize** element is an optional element that specifies either the size, in characters, of the string returned in the **MIMEData** element (section [2.2.2.50](#Section_6d4ca3d1b5424ee189633867089468fb)), if the server returns untruncated [**MIME**](#gt_af6ba277-34c1-493d-8103-71d2af36ce30) data, or the original size, in characters, of the MIME data, if the server returns truncated MIME data. This element is defined in the **Email** namespace as a child of the **airsync:ApplicationData** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.11) in **Sync** command responses (\[MS-ASCMD\] section 2.2.1.21).

The value of this element is an **integer** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

This element is returned by the server only if the client enables MIME support by setting the **airsync:MIMESupport** element (\[MS-ASCMD\] section 2.2.3.110.3) to a nonzero value.

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

#### MIMETruncated

The **MIMETruncated** element is an optional element that indicates whether the **MIMEData** element (section [2.2.2.50](#Section_6d4ca3d1b5424ee189633867089468fb)) contains truncated data. This element is defined in the **Email** namespace as a child of the **airsync:ApplicationData** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.11) in **Sync** command responses (\[MS-ASCMD\] section 2.2.1.21).

The value of this element is a **boolean** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.1. A value of 1 indicates that the [**MIME**](#gt_af6ba277-34c1-493d-8103-71d2af36ce30) data has been truncated by the server; a value of 0 (zero) indicates that the MIME data has not been truncated.

This element is returned by the server only if the client enables MIME support by setting the **airsync:MIMESupport** element (\[MS-ASCMD\] section 2.2.3.110.3) to a nonzero value.

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

#### MonthOfYear

The **MonthOfYear** element is an optional child element of the **Recurrence** element (section [2.2.2.60](#Section_a033deda0745428db9cad08f3e6febcd)) that specifies the month of the year in which the meeting recurs. It is defined as an element in the **Email** namespace.

The value of this element is an **integer** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

This element is required when the **Type** element (section [2.2.2.80](#Section_df2f263ad570471883d080f2b5a2576e)) is set to a value of 6, indicating that the meeting recurs yearly on the Nth day of the week of the Nth month.

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

The **airsyncbase:NativeBodyType** element is an optional element that specifies how the e-mail message is stored on the server. It is defined as an element in the **AirSyncBase** namespace.

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

The **Occurrences** element is an optional child element of the **Recurrence** element (section [2.2.2.60](#Section_a033deda0745428db9cad08f3e6febcd)) that specifies the number of occurrences before the series of recurring meetings ends. It is defined as an element in the **Email** namespace.

The value of this element is an **integer** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

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

#### OrdinalDate

The **tasks:OrdinalDate** element is an optional child element of the **Flag** element (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16)) that identifies the time at which the client set the flag. It is defined as an element in the **Tasks** namespace.

The value of this element is a **dateTime** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.3.

A maximum of one **tasks:OrdinalDate** child element is allowed per **Flag** element. The result of including more than one **tasks:OrdinalDate** child element per **Flag** element is undefined. The server MAY return a protocol status error in response to such a command request.

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

#### Organizer

The **Organizer** element is an optional child element of the **MeetingRequest** element (section [2.2.2.21](#Section_588e67a134184965aa3dfb3e7d5efdab)) that specifies the coordinator of the meeting. It is defined as an element in the **Email** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

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

#### Read

The **Read** element is an optional element that specifies whether the e-mail message has been viewed by the current [**recipient**](#gt_53dfe4f3-05d0-41aa-8217-ecd1962b340b). It is defined as an element in the **Email** namespace.

A value of 1 (TRUE) indicates the e-mail message has been viewed by the current recipient; a value of 0 (zero, meaning FALSE) indicates the e-mail message has not been viewed by the current recipient.

The value of this element is a **boolean** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.1. If a non-**boolean** value is used in a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21), the server responds with **Status** element (\[MS-ASCMD\] section 2.2.3.177.17) value of 6 in the **Sync** command response.

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

#### ReceivedAsBcc

The **email2:ReceivedAsBcc** element is an optional element that indicates to the user that they are a [**blind carbon copy (Bcc) recipient**](#gt_f5634b00-a1bf-4143-bb4f-9cd9dbad2bc0) on the email. It is defined as an element in the **Email2** namespace.

The value of this element is a **boolean** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.1.

Clients MUST NOT change the **email2:ReceivedAsBcc** element value. If the client changes this element value, the server responds with **Status** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.177.17) value of 6 in the **Sync** command response (\[MS-ASCMD\] section 2.2.1.21).

The **email2:ReceivedAsBcc** element is not included in the command response if the value is 0 (zero, meaning FALSE).

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

#### Recurrence

The **Recurrence** element is a **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that defines when and how often the meeting recurs. It is defined as an element in the **Email** namespace.

The **Recurrence** element is a required child element of the **Recurrences** element (section [2.2.2.62](#Section_8722b485daac45aea64035241de8233c)).

The **Recurrence** element can contain the following child elements:

-   **Type** (section [2.2.2.80](#Section_df2f263ad570471883d080f2b5a2576e)): One instance of this element is required.

-   **Interval** (section [2.2.2.41](#Section_fe46604894324a86917498c1943c2a2f)): One instance of this element is required.

-   **Until** (section [2.2.2.85](#Section_d291b0fe6fb84b9f8a08a763f1f70d86)): This element is optional.

-   **Occurrences** (section [2.2.2.55](#Section_7cb2ce2fa83642a8bebf82c94c699ed1)): This element is optional.

-   **WeekOfMonth** (section [2.2.2.88](#Section_08abd347d9734a1cbecbdde8679ebc78)): This element is optional.

-   **DayOfMonth** (section [2.2.2.25](#Section_87154586dfcb4452a0f98605f9c59212)): This element is optional.

-   **DayOfWeek** (section [2.2.2.26](#Section_9dc2f54241ef46598b0591ab96c8cb33)): This element is optional.

-   **MonthOfYear** (section [2.2.2.53](#Section_9a39d7f905a1407f9ddb93d0fd0eecd1)): This element is optional.

-   **email2:CalendarType** (section [2.2.2.15](#Section_fddb63ff4832450c9ef022239c4b9d37)): This element is optional.

-   **email2:IsLeapMonth** (section [2.2.2.43](#Section_a1d32fda006840cb9fc47f9d4124aa65)): This element is optional.

-   **email2:FirstDayOfWeek** (section [2.2.2.33](#Section_415cfef0426541aca00ffcfbdef5d91b)): This element is optional.

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

#### RecurrenceId

The **RecurrenceId** element is an optional child element of the **MeetingRequest** element (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)) that specifies the date and time of this instance of a recurring meeting. It is defined as an element in the **Email** namespace.

The server MUST include this element in response messages to indicate a single instance exception to a recurring meeting; otherwise, the server MUST NOT include this element.

The value of this element MUST be the date corresponding to this instance of a recurring item, and SHOULD include the original start time of the instance if possible.

The value of this element is a **dateTime** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.3.

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

In protocol versions 16.0 and 16.1, the **RecurrenceId** element is also returned for [**orphan instances**](#gt_0efee4a8-a2e9-48fe-87f8-d45097de6b72).

#### Recurrences

The **Recurrences** element is an optional **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that contains details about the recurrence pattern of the meeting. It is a child element of the **MeetingRequest** element (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)) and is defined as an element in the **Email** namespace.

If the **Recurrences** element is included as a child element of the **MeetingRequest** element, it indicates that the meeting has a [**recurrence pattern**](#gt_4275047f-9935-46db-b9b8-8ca605d16649).

The **Recurrences** element MUST contain the following child element:

-   **Recurrence** (section [2.2.2.60](#Section_a033deda0745428db9cad08f3e6febcd)): This element is required.

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

#### Reminder

The **Reminder** element is an optional child element of the **MeetingRequest** element (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)) that specifies the number of seconds prior to the calendar item\'s start time that a reminder will be displayed. It is defined as an element in the **Email** namespace.

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

#### ReminderSet

The **tasks:ReminderSet** element is an optional child element of the **Flag** element (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16)) that specifies whether a reminder has been set for the task. It is defined as an element in the **Tasks** namespace.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The value of this element is set to 1 if a reminder has been set for the task; otherwise, the value of this element is set to 0 (zero). The default value is 0 (zero).

A maximum of one **tasks:ReminderSet** child element is allowed per **Flag** element. The result of including more than one **tasks:ReminderSet** child element per **Flag** element is undefined. The server MAY return a protocol status error in response to such a command request.

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

#### ReminderTime

The **tasks:ReminderTime** element is an optional child element of the **Flag** element (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16)) that identifies the date and time that the reminder is scheduled to occur. It is defined as an element in the **Tasks** namespace.

The value of this element is a **dateTime** value, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.3.

The **tasks:ReminderTime** element MUST be set if the **tasks:ReminderSet** element value is set to 1 (TRUE). The server returns a **Status** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.177.17) value of 6 in the **Sync** command response (\[MS-ASCMD\] section 2.2.1.21) if the **tasks:ReminderSet** element value is set to 1 (TRUE) and the **tasks:ReminderTime** element is not included in the **Sync** command request.

A maximum of one **tasks:ReminderTime** child element is allowed per **Flag** element. The result of including more than one **tasks:ReminderTime** child element per **Flag** element is undefined. The server MAY return a protocol status error in response to such a command request.

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

#### ReplyTo

The **ReplyTo** element is an optional element that specifies the e-mail address(es) to which replies will be addressed by default. It is defined as an element in the **Email** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7. The value of this element contains one or more e-mail addresses. If there are multiple e-mail addresses, they are separated by a semi-colon.

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

#### ResponseRequested

The **ResponseRequested** element is an optional child element of the **MeetingRequest** element ([2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)) that specifies whether the organizer has requested a response to the meeting request. It is defined as an element in the **Email** namespace.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

A **ResponseRequested** element value of 1 indicates that a response is requested; a **ResponseRequested** element value of 0 (zero) indicates that a response is not requested.

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

#### RightsManagementLicense

The **rm:RightsManagementLicense** element is an optional **container** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.2) element that encapsulates the rights management settings for the e-mail item. The **rm:RightsManagementLicense** element and its child elements are defined as part of the **RightsManagement** namespace, as specified in [\[MS-ASRM\]](%5bMS-ASRM%5d.pdf#Section_71e681b7e1784c1096b678df7fa77dfc) section 2.2.2.14.

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

#### Send

The **email2:Send** element is an optional element that specifies whether an email is to be saved as a draft or sent. It is defined as an element of the **Email2** namespace. This element is a child of **airsync:Add** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.7.2) or the **airsync:Change** element (\[MS-ASCMD\] section 2.2.3.24) in **Sync** command requests (\[MS-ASCMD\] section 2.2.1.21).

The **Send** element is an empty tag element, meaning it has no value or data type. It is distinguished only by the presence or absence of the \<Send/\> tag. The presence of the tag in a **Sync** command request indicates that the email is to be sent; the absence of the tag indicates that the email is to be saved as a draft.

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

#### Sender

The **email2:Sender** element is an optional element that indicates the message was not sent from the user identified by the **From** element (section [2.2.2.36](#Section_bf2394e29c1b45ec899b0fba04f7c1e7)). It is defined as an element in the **Email2** namespace.

The value of the **Sender** element is an **e-mail address**, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.3.

This element is set by the server and is read-only on the client. If the client attempts to change this value, the server returns a **Status** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.177.17) value of 6 in the **Sync** command response (\[MS-ASCMD\] section 2.2.1.21).

If present, the **email2:Sender** element identifies the user that actually sent the message, and the **From** element identifies the user on whose behalf the message was sent. Use of the **email2:Sender** element indicates that the sender of the item had [**delegate**](#gt_eeac1cee-185f-47d9-ace5-555e3a2a6930) access to the **From** user\'s [**mailbox**](#gt_d3ad0e15-adc9-4174-bacf-d929b57278b3).

The **email2:Sender** element is not sent to the client when the **email2:Sender** element and the **From** element have the same value, or when the **email2:Sender** element value is NULL.

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

The **Sensitivity** element is an optional child element of the **MeetingRequest** element (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)) that specifies the confidentiality level of the meeting request. It is defined as an element in the **Email** namespace.

The value of this element is an **integer** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

The value of this element MUST be one of the values listed in the following table.

  -----------------------------------------------------------------------
  Value                       Meaning
  --------------------------- -------------------------------------------
  0                           Normal

  1                           Personal

  2                           Private

  3                           Confidential
  -----------------------------------------------------------------------

If this element not present, then a default of 0 (zero) MUST be assumed.

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

#### StartDate

The **tasks:StartDate** element is an optional child element of the **Flag** element (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16)) that specifies when the flagged item was begun. It is defined as an element in the **Tasks** namespace.

When a flag is updated, the **tasks:StartDate** element value MUST NOT occur after the **tasks:DueDate** element (section [2.2.2.31](#Section_cc1cc4674ecd4ef68ee8d0e52e9da8b3)) value. The server returns a **Status** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.177.17) value of 6 in the **Sync** command response (\[MS-ASCMD\] section 2.2.1.21) if this condition is not met.

To set a flag, the **tasks:StartDate** element, **tasks:DueDate** element, **tasks:UtcStartDate** element (section [2.2.2.87](#Section_d671fbb9aa284cec8a69089935c504a8)), and **tasks:UtcDueDate** element (section [2.2.2.86](#Section_07e6f69fc7a444a1b0254ec85cd83a0d)) either all MUST be set, or all MUST be NULL. The server returns a **Status** element (\[MS-ASCMD\] section 2.2.3.177.17) value of 6 in the **Sync** command response if this condition is not met.

The value of this element is a **dateTime** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.3.

A maximum of one **tasks:StartDate** child element is allowed per **Flag** element. The result of including more than one **tasks:StartDate** child element per **Flag** element is undefined. The server MAY return a protocol status error in response to such a command request.

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

#### StartTime

The **StartTime** element is an optional child element of the **MeetingRequest** element (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)) that specifies when the meeting begins. It is defined as an element in the **Email** namespace.

The value of this element is a **dateTime** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.3.

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

#### Status

The **Status** element is an optional child element of the **Flag** element (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16)) that specifies the current status of the flag. It is defined as an element in the **Email** namespace.

The value of this element is an **integer** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

The value of this element MUST be one of the values in the following table.

  -----------------------------------------------------------------------
  Value           Meaning
  --------------- -------------------------------------------------------
  0               The flag is cleared.

  1               The status is set to complete.

  2               The status is set to active.
  -----------------------------------------------------------------------

The server returns a **Status** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.177.17) value of 6 in the **Sync** command response (\[MS-ASCMD\] section 2.2.1.21) if the flag **Status** element is set to a value other than 0, 1, or 2.

A maximum of one **Status** element is allowed per **Flag**. The result of including more than one **Status** element per **Flag** is undefined. The server MAY return a protocol status error in response to such a command request.

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

#### Subject

The **E-mail** class uses both the **email:Subject** element, as specified in section [2.2.2.75.1](#Section_cf3cc4bb765b467195095a16a9184779), and the **tasks:Subject** element, as specified in section [2.2.2.75.2](#Section_6163992744994a5484d7935a9b591f56).

##### Subject (Email Namespace)

As a top-level element of the **E-mail** class, the **Subject** element is an optional element that specifies the subject of the e-mail message. It is defined as an element in the **Email** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

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

##### Subject (Tasks Namespace)

The **tasks:Subject** element is an optional child element of the **Flag** element (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16)). It specifies the subject of the flag. It is defined as an element in the **Tasks** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

The client or server SHOULD set the value of the **tasks:Subject** element to the subject of the message when an item is flagged. A maximum of one **tasks:Subject** child element is allowed per **Flag** element.

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

#### SubOrdinalDate

The **tasks:SubOrdinalDate** element is an optional child element of the **Flag** element (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16)) that specifies a value that SHOULD be used for sorting. It is defined as an element in the **Tasks** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7. It can contain any **string** value and SHOULD be used for sorting if there are duplicate **tasks:OrdinalDate** element (section [2.2.2.56](#Section_be2830cb41ab470a89a288eeddc1c4a2)) values.

A maximum of one **tasks:SubOrdinalDate** child element is allowed per **Flag** element. The result of including more than one **tasks:SubOrdinalDate** child element per **Flag** element is undefined. The server MAY return a protocol status error in response to such a command request.

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

#### ThreadTopic

The **ThreadTopic** element is an optional element that specifies the topic used for conversation threading. It is defined as an element in the **Email** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7.

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

#### TimeZone

The **TimeZone** element is a required child element of the **MeetingRequest** element (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)) that specifies the time zone specified when the calendar item was created. It is defined as an element in the **Email** namespace.

The value of this element is a **string** data type ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7) in **TimeZone** format, as specified in \[MS-ASDTYPE\] section 2.7.6.

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

#### To

The **To** element is an optional element that specifies the list of primary [**recipients**](#gt_53dfe4f3-05d0-41aa-8217-ecd1962b340b) of a message. It is defined as an element in the **Email** namespace.

The value of this element contains one or more e-mail addresses. If there are multiple e-mail addresses, they are separated by commas.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7, and has a maximum length of 32,768 characters.

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

#### Type

The **Type** element is a required child element of the **Recurrence** element (section [2.2.2.60](#Section_a033deda0745428db9cad08f3e6febcd)) that specifies how the meeting recurs. It is defined as an element in the **Email** namespace.

The value of this element is an **unsignedByte** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.8.

The value of this element MUST be one of the values in the following table.

  ------------------------------------------------------------------------------------------
  Value   Meaning
  ------- ----------------------------------------------------------------------------------
  0       The meeting recurs daily.

  1       The meeting recurs weekly.

  2       The meeting recurs monthly on the Nth day of the month.

  3       The meeting recurs monthly.

  5       The meeting recurs yearly on the Nth day of the Nth month each year.

  6       The meeting recurs yearly on the Nth day of the week of the Nth month each year.
  ------------------------------------------------------------------------------------------

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

#### UmAttDuration

The **email2:UmAttDuration** element is an optional child element of the **airsyncbase:Attachment** element ([\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.7) that specifies the duration of the most recent electronic voice mail attachment in seconds. It is defined as an element in the **Email2** namespace.

The value of this element is an **integer** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

This element MUST only be used for electronic voice message attachments. This value is set by the server and is read-only for the client.

This element MUST only be included for messages with a **MessageClass** element (section [2.2.2.49](#Section_51d84da6a2da41e98ca7eb6c4e72c28d)) value that begins with the prefix of \"IPM.Note.Microsoft.Voicemail\", \"IPM.Note.RPMSG.Microsoft.Voicemail\", or \"IPM.Note.Microsoft.Missed.Voice\".

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

#### UmAttOrder

The **email2:UmAttOrder** element is an optional child element of the **airsyncbase:Attachment** element ([\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.7) that identifies the order of electronic voice mail attachments. It is defined as an element in the **Email2** namespace.

The value of this element is an **integer** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

This value is set by the server and is read-only for the client.

The most recent voice mail attachment in an e-mail item MUST have an **email2:UmAttOrder** value of 1. Whenever a new electronic voice message associated with the same e-mail item is received, the new voice attachment is appended to the end of the list and all electronic voice attachments are renumbered.

This element MUST only be included for messages with a **MessageClass** element (section [2.2.2.49](#Section_51d84da6a2da41e98ca7eb6c4e72c28d)) value that begins with the prefix of \"IPM.Note.Microsoft.Voicemail\", \"IPM.Note.RPMSG.Microsoft.Voicemail\", or \"IPM.Note.Microsoft.Missed.Voice\".

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

#### UmCallerID

The **email2:UmCallerID** element is an optional element that specifies the callback telephone number of the person who called or left an electronic voice message. It is defined as an element in the **Email2** namespace.

The **string** ([\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7) value of this element is either formatted as an E.164 telephone number, as specified in [\[E164\]](https://go.microsoft.com/fwlink/?LinkId=89855), or as a session initiated protocol link to initiate a [**Voice over IP (VoIP)**](#gt_f8d6223d-5289-4966-9fc0-8ec7b7b42860) call. For more details about session initiated protocol links, see [\[RFC3261\]](https://go.microsoft.com/fwlink/?LinkId=90410).

This element is sent from the server to the client, and MUST NOT be sent from the client to the server. The server returns a **Status** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.177.17) value of 6 in the **Sync** command response (\[MS-ASCMD\] section 2.2.1.21) if the client attempts to send the **email2:UmCallerId** element to the server. The **email2:UmCallerID** element is not included in the command response, or is empty in the command response, if the call originated as a private, blocked, or otherwise anonymous call meaning the caller's telephone number was masked and unavailable to the recipient of the call.

This element MUST only be included for messages with one of the following **MessageClass** values:

-   IPM.Note.Microsoft.Voicemail

-   IPM.Note.Microsoft.Voicemail.UM

-   IPM.Note.Microsoft.Voicemail.UM.CA

-   IPM.Note.RPMSG.Microsoft.Voicemail

-   IPM.Note.RPMSG.Microsoft.Voicemail.UM

-   IPM.Note.RPMSG.Microsoft.Voicemail.UM.CA

-   IPM.Note.Microsoft.Missed.Voice

Only one **email2:UmCallerID** element is allowed per message. In order to enable future VoIP scenarios, the server SHOULD send this element to the client regardless of the client\'s current VoIP capabilities.

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

#### UmUserNotes

The **email2:UmUserNotes** element is an optional element that contains user notes related to an electronic voice message. It is defined as an element in the **Email2** namespace.

The value of this element is a **string** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.7. The server truncates notes larger than 16,374 bytes, to 16,374 bytes.

This element is sent from the server to the client, and MUST NOT be sent from the client to the server. The server returns a **Status** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.177.17) value of 6 in the **Sync** command response (\[MS-ASCMD\] section 2.2.1.21) if the client attempts to send the **email2:UmUserNotes** element to the server.

This element MUST only be included for electronic voice messages with one of the following **MessageClass** values:

-   IPM.Note.Microsoft.Voicemail

-   IPM.Note.Microsoft.Voicemail.UM

-   IPM.Note.Microsoft.Voicemail.UM.CA

-   IPM.Note.RPMSG.Microsoft.Voicemail

-   IPM.Note.RPMSG.Microsoft.Voicemail.UM

-   IPM.Note.RPMSG.Microsoft.Voicemail.UM.CA

-   IPM.Note.Microsoft.Missed.Voice

Only one **email2:UmUserNotes** element is allowed per message.

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

#### Until

The **Until** element is an optional child element of the **Recurrence** element (section [2.2.2.60](#Section_a033deda0745428db9cad08f3e6febcd)) that specifies the end date and time of a recurring meeting. It is defined as an element in the **Email** namespace.

The value of this element is a **string** value, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section [2.7](%5bMS-ASDTYPE%5d.docx#Section_48f087f18724498ab80e15c1956ab3fe).

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

#### UtcDueDate

The **tasks:UtcDueDate** element is an optional child element of the **Flag** element (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16)) that contains the [**UTC**](#gt_f2369991-a884-4843-a8fa-1505b6d5ece7) value of the local **tasks:DueDate** element (section [2.2.2.31](#Section_cc1cc4674ecd4ef68ee8d0e52e9da8b3)) value. It is defined as an element in the **Tasks** namespace.

When a flag is updated, the **tasks:UtcDueDate** element value MUST NOT occur before the **tasks:UtcStartDate** element (section [2.2.2.87](#Section_d671fbb9aa284cec8a69089935c504a8)) value. The server returns a **Status** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.177.17) value of 6 in the **Sync** command response (\[MS-ASCMD\] section 2.2.1.21) if this condition is not met.

To set a flag, the **tasks:StartDate** element (section [2.2.2.72](#Section_98e703d64d254727896d49f555465aba)), **tasks:DueDate** element, **tasks:UtcStartDate** element, and **tasks:UtcDueDate** element either all MUST be set, or all MUST be NULL. The server returns a **Status** element value of 6 in the **Sync** command response if this condition is not met.

The value of this element is a **dateTime** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.3.

A maximum of one **tasks:UtcDueDate** child element is allowed per **Flag** element. The result of including more than one **tasks:UtcDueDate** child elements per **Flag** element is undefined. The server MAY return a protocol status error in response to such a command request.

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

#### UtcStartDate

The **tasks:UtcStartDate** element is an optional child element of the **Flag** element (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16)) that contains the [**UTC**](#gt_f2369991-a884-4843-a8fa-1505b6d5ece7) value of the local **tasks:StartDate** element (section [2.2.2.72](#Section_98e703d64d254727896d49f555465aba)) value. It is defined as an element in the **Tasks** namespace.

When a flag is updated, the **tasks:UtcStartDate** element value MUST occur before the **tasks:UtcDueDate** element (section [2.2.2.86](#Section_07e6f69fc7a444a1b0254ec85cd83a0d)) value. The server returns a **Status** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.177.17) value of 6 in the **Sync** command response (\[MS-ASCMD\] section 2.2.1.21) if this condition is not met.

To set a flag, the **tasks:StartDate** element, **tasks:DueDate** element (section [2.2.2.31](#Section_cc1cc4674ecd4ef68ee8d0e52e9da8b3)), **tasks:UtcStartDate** element, and **tasks:UtcDueDate** element either all MUST be set, or all MUST be NULL. The server returns a **Status** element value of 6 in the **Sync** command response if this condition is not met.

The value of this element is a **dateTime** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.3.

A maximum of one **tasks:UtcStartDate** child element is allowed per **Flag** element. The result of including more than one **tasks:UtcStartDate** child elements per **Flag** element is undefined. The server MAY return a protocol status error in response to such a command request.

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

#### WeekOfMonth

The **WeekOfMonth** element is an optional child element of the **Recurrence** element (section [2.2.2.60](#Section_a033deda0745428db9cad08f3e6febcd)) that specifies the week of the month in which the meeting recurs. It is defined as an element in the **Email** namespace.

The value of this element is an **integer** data type, as specified in [\[MS-ASDTYPE\]](%5bMS-ASDTYPE%5d.pdf#Section_dcfe20e1cb36457f8c7be5c61351f7d3) section 2.6.

This element is required when the **Type** element (section [2.2.2.80](#Section_df2f263ad570471883d080f2b5a2576e)) value is set to 6 (indicating that the meeting recurs yearly on the Nth day of the week during the Nth month each year).

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
  **TopLevelSchemaProps** (section [2.2.3.1](#Section_ed855dd83fa24296bb1e7dbc4c0b83ea))   Identifies the elements that are part of the **TopLevelSchemaProps** group.

  ----------------------------------------------------------------------------------------------------------------------------------------------------------------------

#### TopLevelSchemaProps

The **TopLevelSchemaProps** group identifies the following elements as being part of the TopLevelSchemaProps group:

**To**, as specified in section [2.2.2.79](#Section_86168c1043494946a8bb9ce39fd9bb24)

**Cc**, as specified in section [2.2.2.18](#Section_0a808a7f3e8845ad98814549ca85c008)

**From**, as specified section [2.2.2.36](#Section_bf2394e29c1b45ec899b0fba04f7c1e7)

**ReplyTo**, as specified in section [2.2.2.66](#Section_2a5e10c7591d44899f1976d5af28c383)

**DateReceived**, as specified in section [2.2.2.24](#Section_51c28fa0b2884b85a9e1db5b5a44fc1f)

**Subject**, as specified in section [2.2.2.75.1](#Section_cf3cc4bb765b467195095a16a9184779)

**DisplayTo**, as specified in section [2.2.2.29](#Section_237c7acd4e0b465087c43e19989aa22c)

**Importance**, as specified section [2.2.2.38](#Section_f98d35bd5638410a83490fec21aea6f7)

**Read**, as specified in section [2.2.2.58](#Section_7dce52172fb147508c5216a709aa02cf)

**MessageClass**, as specified in section [2.2.2.49](#Section_51d84da6a2da41e98ca7eb6c4e72c28d)

**MeetingRequest**, as specified in section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)

**ThreadTopic**, as specified section [2.2.2.77](#Section_623a044774c74145849f8732f8f0efdf)

**InternetCPID**, as specified in section [2.2.2.40](#Section_5152748b0d054b9bafeba1bb83d73439)

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

**E-mail class:** A set of [**XML elements**](#gt_a364f92c-0374-4568-b7f8-40bd74437dd5) that specifies an e-mail message and adheres to the schema definition specified in section [2.2](#Section_570afbfba0ee45df90a0dbea8e062bf7). **E-mail** class data is included in command requests sent to the server when e-mail messages need to be retrieved or synchronized. For more details about processing command requests, see section [3.1.5](#Section_409227c4deb642eda628ccd61b4fe388).

**Command request:** A [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc)-formatted message that adheres to the command schemas specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

### Timers

None.

### Initialization

None.

### Higher-Layer Triggered Events

#### Synchronizing E-Mail Data Between Client and Server

A client initiates synchronization of **E-mail** class data with the server by sending a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) to the server, as specified in section [3.1.5.4](#Section_a8602ea5a3f3442683b5a8d5315a953d).

#### Sending E-Mail

A client sends an e-mail message by sending a **SendMail** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.17) to the server.

#### Searching a Server for E-Mail Data

A client searches for **E-mail** class data on the server by sending a **Search** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.16) to the server, as specified in section [3.1.5.3](#Section_f479b9297e5b447aa898a8323439f131) or by sending a **Find** command request (\[MS-ASCMD\] section 2.2.1.2) to the server, as specified in section [3.1.5.1](#Section_8f107a08fbc54f6fa942ce0841f3de92).

#### Retrieving Data for One or More E-Mail Items

A client requests **E-mail** class data for one or more individual e-mail items by sending an **ItemOperations** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10) to the server that contains one or more **itemoperations:Fetch** elements (\[MS-ASCMD\] section 2.2.3.67.1), as specified in section [3.1.5.2](#Section_f83d87c403554adebb31cc1e8df9e1b6).

#### Sending and Receiving Meeting Requests

When a user creates an appointment or meeting on the client, the calendar item is added to the server by using the **Sync** command ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21). In protocol versions 2.5, 12.0, 12.1, 14.0, and 14.1, if the meeting has attendees, the client uses the **SendMail** command request (\[MS-ASCMD\] section 2.2.1.17) to send meeting requests to the attendees. When an attendee's [**Inbox folder**](#gt_baa08600-0402-47f6-a8ce-9690cf962c96) is synchronized, the **Sync** command response (\[MS-ASCMD\] section 2.2.1.21) from the server contains the new meeting request that is to be added to the attendee\'s Inbox folder. When an attendee's [**Calendar folder**](#gt_60b55610-ca65-41f2-91d8-a4d6f4cc6d20) is synchronized, the **Sync** command response from the server contains the new calendar item that is to be added to the attendee\'s Calendar folder. For an example that demonstrates the process of uploading a meeting to the server, sending meeting request, adding a meeting request to an attendee's Inbox folder, and adding a meeting to an attendee's Calendar folder, see \[MS-ASCMD\] section 4.16.

#### Updating E-Mail Flags on the Server

A client SHOULD add a flag to an e-mail message, mark a flag on an e-mail message as complete, or clear a flag from an e-mail message by sending a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) to the server, as specified in sections [3.1.5.4](#Section_a8602ea5a3f3442683b5a8d5315a953d) and [3.1.5.4.1](#Section_7b06838fa6234f4fafafeca46325459c).

#### Determining Whether a Meeting Request Corresponds to an Existing Calendar Object

Clients need to determine whether the **GlobalObjId** element (section [2.2.2.37](#Section_9f00dbd2c0e5406c8d7452fffd0bbfd3)) value for a meeting request corresponds to an existing [**Calendar object**](#gt_b9ce8e55-dae6-467b-b5dc-850087d4dc18) in the [**Calendar folder**](#gt_60b55610-ca65-41f2-91d8-a4d6f4cc6d20). When protocol version 2.5, 12.0, 12.1, 14.0, or 14.1 is used, clients need to convert the **GlobalObjId** element value to a **calendar:UID** element value ([\[MS-ASCAL\]](%5bMS-ASCAL%5d.pdf#Section_0c4486824a6a459aae662fed0712bef9) section 2.2.2.46) to make the comparison of the unique identifier of the meeting request and the unique identifier of the calendar item. When protocol version 16.0 or 16.1 is used, no conversion is necessary.

In protocol versions 2.5, 12.0, 12.1, 14.0, and 14.1, the following process SHOULD be used to convert the **GlobalObjId** element value to a **UID** value:

1.  Decode the **GlobalObjId** element value, assuming [**base64 encoding**](#gt_179b9392-9019-45a3-880b-26f6890522b7), to determine its length.

2.  Determine whether the **GlobalObjId** element value is an OutlookID[\<2\>](\l) or a vCal ID. A vCal ID is an identifier set in accordance with the guidelines specified by [\[RFC2445\]](https://go.microsoft.com/fwlink/?LinkId=112504), [\[RFC2446\]](https://go.microsoft.com/fwlink/?LinkId=90354), and [\[RFC2447\]](https://go.microsoft.com/fwlink/?LinkId=193299). The **GlobalObjId** element value is an OutlookID if any of the following conditions are true:

    -   If the length of the **GlobalObjId** element value is less than 53 bytes.

    -   If bytes 41-48 of the **GlobalObjId** element value do not equal \"vCal-Uid\".

    -   If the value of bytes 37-40, which represent the length of the data that follows, are less than 13 or greater than the remaining length of the **GlobalObjId** element value.

3.  If the **GlobalObjId** element value is an OutlookID, hex encode the entire base64 decoded **GlobalObjID** element value, and zero out bytes 17-20. The resulting value is the **UID** value.

4.  If the **GlobalObjId** element value is a vCal ID, the length of the **UID** value to be extracted is equal to the length of the data, as specified bytes 37-40, minus 12 bytes for the vCal marker, minus one byte for null-terminating **00** byte at the end of the vCal ID.

5.  Using the length of the **UID** value calculated in the previous step, extract that number of bytes beginning at byte 53. These extracted bytes are the **UID** value.

For an example of this process, see section [4.3](#Section_e7424ddcdd10431ea0b75c794863370e).

In protocol versions 16.0 and 16.1, clients SHOULD use the following procedure to correlate a meeting request to a calendar item:

1.  Compare the value of the **calendar:UID** element that is provided in the **MeetingRequest** element (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df)) of a command response to the unique identifier of one or more calendar items. In the case that a user has been invited to multiple [**orphan instances**](#gt_0efee4a8-a2e9-48fe-87f8-d45097de6b72), multiple calendar items will exist with the same unique identifier; otherwise zero or one match will exist.

2.  If the **MeetingRequest** element contains a **RecurrenceId** element (section [2.2.2.61](#Section_262bccb1697e4d2c8146227005bcb4b9)) and the **InstanceType** element (section [2.2.2.39](#Section_a4d4823969364b229267623ecdb49b5e)) with a value of 4 (signifying this is an orphan instance), compare the **RecurrenceId** element value to the InstanceId property of each calendar item to identify the matching orphan instance.

3.  If the **MeetingRequest** element contains a **RecurrenceId** element and the **InstanceType** element with a value of 2 or 3 (signifying this is an instance of a [**recurring series**](#gt_2325d666-e02f-49e4-afa5-3e896d672efe)), the calendar item that was matched in step 1 represents the recurring series. In this case, the **RecurrenceId** element is used to determine the instance, which can be an exception to the calendar item\'s [**recurrence pattern**](#gt_4275047f-9935-46db-b9b8-8ca605d16649).

### Message Processing Events and Sequencing Rules

The following sections specify how elements of the **E-mail** class are used in the context of specific ActiveSync commands. Command details are specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

#### Find Command Request

A client uses the **Find** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.2) to retrieve **E-mail** class items from the server that match the criteria specified by the client.

#### ItemOperations Command Request

A client uses an **ItemOperations** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10) that contains one or more **itemoperations:Fetch** elements (\[MS-ASCMD\] section 2.2.3.67.1) to retrieve data from the server for one or more specific e-mail items.

Only the following elements of the **E-mail** class can be included in an **ItemOperations** command request:

-   **To** (section [2.2.2.79](#Section_86168c1043494946a8bb9ce39fd9bb24))

-   **Cc** (section [2.2.2.18](#Section_0a808a7f3e8845ad98814549ca85c008))

-   **From** (section [2.2.2.36](#Section_bf2394e29c1b45ec899b0fba04f7c1e7))

-   **ReplyTo** (section [2.2.2.66](#Section_2a5e10c7591d44899f1976d5af28c383))

-   **DateReceived** (section [2.2.2.24](#Section_51c28fa0b2884b85a9e1db5b5a44fc1f))

-   **Subject** (section [2.2.2.75.1](#Section_cf3cc4bb765b467195095a16a9184779))

-   **DisplayTo** (section [2.2.2.29](#Section_237c7acd4e0b465087c43e19989aa22c))

-   **Importance** (section [2.2.2.38](#Section_f98d35bd5638410a83490fec21aea6f7))

-   **Read** (section [2.2.2.58](#Section_7dce52172fb147508c5216a709aa02cf))

-   **MessageClass** (section [2.2.2.49](#Section_51d84da6a2da41e98ca7eb6c4e72c28d))

-   **MeetingRequest** (section [2.2.2.48](#Section_0bc0a4c72391428db2c1471c6695d1df))

-   **ThreadTopic** (section [2.2.2.77](#Section_623a044774c74145849f8732f8f0efdf))

-   **InternetCPID** (section [2.2.2.40](#Section_5152748b0d054b9bafeba1bb83d73439))

If included in an **ItemOperations** command request, each of these elements MUST be transmitted as a child element of the **itemoperations:Schema** element (\[MS-ASCMD\] section 2.2.3.158).

For more details about the **ItemOperations** command, see \[MS-ASCMD\] section 2.2.1.10.

#### Search Command Request

A client uses the **Search** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.16) to retrieve **E-mail** class items from the server that match the criteria specified by the client.

Elements that belong to the **E-mail** class, as specified in section [2.2.2](#Section_c9bf7f72741b40698e93167fa14697b7), MUST NOT be included in a **Search** command request.

#### Sync Command Request

A client uses the **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) to synchronize its **E-mail** class items for a specified user with the e-mail items that are currently stored by the server. Synchronization of draft emails is supported in protocol versions 16.0 and 16.1.

**E-mail** class elements included in a **Sync** command request MUST be transmitted as child elements of the **airsync:ApplicationData** element (\[MS-ASCMD\] section 2.2.3.11) within either an **airsync:Add** element (\[MS-ASCMD\] section 2.2.3.7.2) or an **airsync:Change** element (\[MS-ASCMD\] section 2.2.3.24).

The following **E-mail** class elements can be child elements of the **airsync:ApplicationData** element when the **airsync:ApplicationData** element appears within the **airsync:Change** element in a **Sync** command request to synchronize a non-draft email:

-   **Flag** (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16))

-   **Read** (section [2.2.2.58](#Section_7dce52172fb147508c5216a709aa02cf))

-   **Categories** (section [2.2.2.16](#Section_3b761feb2905427e822182ab60becfc6))

**E-mail** class elements that are child elements of the **airsync:ApplicationData** element when the **airsync:ApplicationData** element appears within the **airsync:Add** element in a **Sync** command request can be used either to synchronize **SMS** class content or to create a draft email on the server, depending on the specified class of the item. For more details about the **SMS** class, see [\[MS-ASMS\]](%5bMS-ASMS%5d.pdf#Section_3123f34aaabe4ec5aa836f6d48698a8b).

When synchronizing a draft email containing an **airsyncbase:Body** element with a child **airsyncbase:Type** element which is not equal to 4 (MIME), the following elements are allowed as child elements of the **airsync:ApplicationData** element when the **airsync:ApplicationData** element appears within either the **airsync:Add** element or the **airsync:Change** element in a **Sync** command request:

-   **To** (section [2.2.2.79](#Section_86168c1043494946a8bb9ce39fd9bb24))

-   **Cc** (section [2.2.2.18](#Section_0a808a7f3e8845ad98814549ca85c008))

-   **Bcc** (section [2.2.2.9](#Section_e6131f5863a449d5b5922830180fd868))

-   **Subject** (section [2.2.2.75.1](#Section_cf3cc4bb765b467195095a16a9184779))

-   **Importance** (section [2.2.2.38](#Section_f98d35bd5638410a83490fec21aea6f7))

-   **ReplyTo** (section [2.2.2.66](#Section_2a5e10c7591d44899f1976d5af28c383))

-   **airsyncbase:Attachments** (section [2.2.2.4.1](#Section_14e0cbaef1c6499ba64c9293c675ef1c))

-   **airsyncbase:Body** (section [2.2.2.10.1](#Section_c2e6f8024947446fa0c6a7fb684a37cf))

-   **Read** (section 2.2.2.58)

-   **Flag** (section 2.2.2.34)

When synchronizing a draft email containing an **airsyncbase:Body** element with a child **airsyncbase:Type** element which is equal to 4 (MIME), the following elements are allowed as child elements of the **airsync:ApplicationData** element when the **airsync:ApplicationData** element appears within either the **airsync:Add** element or the **airsync:Change** element in a **Sync** command request:

-   **Importance** (section 2.2.2.38)

-   **airsyncbase:Attachments** (section 2.2.2.4.1)

-   **airsyncbase:Body** (section 2.2.2.10.1)

-   **Read** (section 2.2.2.58)

-   **Flag** (section 2.2.2.34)

##### Updating E-Mail Flags

A client uses the following elements within a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) to specify flags:

-   **Flag** (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16))

-   **tasks:Subject** (section [2.2.2.75.2](#Section_6163992744994a5484d7935a9b591f56))

-   **Status** (section [2.2.2.74](#Section_20fba2e8af71412abb1b29c8d0d3fd2d))

-   **FlagType** (section [2.2.2.35](#Section_157c6b73940144e9b9e0fbeadef817f6))

-   **tasks:DateCompleted** (section [2.2.2.23](#Section_c4ce6efa4d0040af9bbf1582186dcf59))

-   **CompleteTime** (section [2.2.2.19](#Section_79799c317acb46fcb4c4fdbac218c383))

-   **tasks:StartDate** (section [2.2.2.72](#Section_98e703d64d254727896d49f555465aba))

-   **tasks:DueDate** (section [2.2.2.31](#Section_cc1cc4674ecd4ef68ee8d0e52e9da8b3))

-   **tasks:UtcStartDate** (section [2.2.2.87](#Section_d671fbb9aa284cec8a69089935c504a8))

-   **tasks:UtcDueDate** (section [2.2.2.86](#Section_07e6f69fc7a444a1b0254ec85cd83a0d))

-   **tasks:ReminderSet** (section [2.2.2.64](#Section_e19643a39288466eb691ef98ef60df1a))

-   **tasks:ReminderTime** (section [2.2.2.65](#Section_6e3300c9da1b4f9aad4e95f131fdb140))

-   **tasks:OrdinalDate** (section [2.2.2.56](#Section_be2830cb41ab470a89a288eeddc1c4a2))

-   **tasks:SubOrdinalDate** (section [2.2.2.76](#Section_74a2a826f08f467e9164359d7cf6a3d1))

The following figure shows the life cycle of a flag.

![Flag life cycle. Flag actions clear, set, and mark complete.](media/image1.bin "Flag life cycle"){width="4.368055555555555in" height="2.6131944444444444in"}

Figure 1: Flag life cycle

For details about the flag action (clear, set, mark complete) that the server will invoke when updating flags based upon the information specified in the **Sync** command request, see section [3.2.5.4.2](#Section_56dc3bae2bc94a5cbe69ac5e4ee8f90c).

### Timer Events

None.

### Other Local Events

None.

## Server Details

### Abstract Data Model

This section describes a conceptual model of possible data organization that an implementation maintains to participate in this protocol. The described organization is provided to facilitate the explanation of how the protocol behaves. This document does not mandate that implementations adhere to this model as long as their external behavior is consistent with that described in this document.

**E-mail class:** A set of [**XML elements**](#gt_a364f92c-0374-4568-b7f8-40bd74437dd5) that specifies an e-mail message and adheres to the schema definition specified in section [2.2](#Section_570afbfba0ee45df90a0dbea8e062bf7). **E-mail** class data is returned by the server to the client as part of the full [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85) response to the client requests that are specified in section [3.1.5](#Section_409227c4deb642eda628ccd61b4fe388). For more details about processing command responses, see section [3.2.5](#Section_8a3576adff0849b4bd5dfe0a66d0edb4).

**Command response:** A [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc)-formatted message that adheres to the command schemas specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

### Timers

None.

### Initialization

None.

### Higher-Layer Triggered Events

#### Synchronizing E-Mail Data Between Client and Server

Synchronization of **E-mail** class data between client and server is initiated by the client, as specified in section [3.1.4.1](#Section_b0fefdf458c646f3942be352b44e2357). The server responds with a **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21), as specified in section [3.2.5.4](#Section_03e9386ea7c94b1b92fedf15529e20bc).

#### Searching for E-Mail Data

Searching for **E-mail** class data is initiated by the client, as specified in section [3.1.4.3](#Section_a1710fcd87684e58b7b27bad497399df). The server responds with a **Search** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.16), as specified in section [3.2.5.3](#Section_f6574aa58d1c4a9291682c9556437c18).

#### Retrieving Data for One or More E-Mail Items

Retrieval of **E-mail** class data for one or more e-mail items is initiated by the client, as specified in section [3.1.4.4](#Section_f542b1770df24b9fb13430f9555d1e22). The server responds with an **ItemOperations** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10), as specified in section [3.2.5.2](#Section_8c041a11353841e2917ae41d5936102a).

### Message Processing Events and Sequencing Rules

The following sections specify how elements of the **E-mail** class are used in the context of specific ActiveSync commands. Command details are specified in [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a).

#### Find Command Response

When a client uses the **Find** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.2), as specified in section [3.1.5.1](#Section_8f107a08fbc54f6fa942ce0841f3de92), to retrieve **E-mail** class items from the server that match the criteria specified by the client, the server responds with a **Find** command response.

Any of the elements that belong to the **E-mail** class, as specified in section [2.2.2](#Section_c9bf7f72741b40698e93167fa14697b7), can be included in a **Find** command response as child elements of the **find:Properties** element (\[MS-ASCMD\] section 2.2.3.139.1).

#### ItemOperations Command Response

When a client uses an **ItemOperations** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10), as specified in section [3.1.5.2](#Section_f83d87c403554adebb31cc1e8df9e1b6), to retrieve data from the server for one or more specific e-mail items, the server responds with an **ItemOperations** command response.

Any of the elements that belong to the **E-mail** class, as specified in section [2.2.2](#Section_c9bf7f72741b40698e93167fa14697b7), can be included in an **ItemOperations** command response. If an **airsync:Schema** element (\[MS-ASCMD\] section 2.2.3.158) is included in the **ItemOperations** command request, then the elements returned in the **ItemOperations** command response MUST be restricted to the elements that were included as child elements of the **airsync:Schema** element in the command request.

**E-mail** class elements MUST be returned as child elements of the **itemoperations:Properties** element (\[MS-ASCMD\] section 2.2.3.139.2) in the **ItemOperations** command response.

#### Search Command Response

When a client uses the **Search** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.16), as specified in section [3.1.5.3](#Section_f479b9297e5b447aa898a8323439f131), to retrieve **E-mail** class items from the server that match the criteria specified by the client, the server responds with a **Search** command response.

Any of the elements that belong to the **E-mail** class, as specified in section [2.2.2](#Section_c9bf7f72741b40698e93167fa14697b7), can be included in a **Search** command response as child elements of the **search:Properties** element (\[MS-ASCMD\] section 2.2.3.139.3).

If **E-mail** class elements are included in the **Search** command request, the **Search** command response from the server contains a **search:Status** element (\[MS-ASCMD\] section 2.2.3.177.13) value of 2 as a child element of the **search:Store** element (\[MS-ASCMD\] section 2.2.3.178.3).

#### Sync Command Response

When a client uses the **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21), as specified in section [3.1.5.4](#Section_a8602ea5a3f3442683b5a8d5315a953d), to synchronize its **E-mail** class items for a specified user with the e-mail items that are currently stored by the server, the server responds with a **Sync** command response.

Any of the elements that belong to the **E-mail** class, as specified in section [2.2.2](#Section_c9bf7f72741b40698e93167fa14697b7), can be included in a **Sync** command response.

**E-mail** class elements MUST be returned as child elements of the **airsync:ApplicationData** element (\[MS-ASCMD\] section 2.2.3.11) within either an **airsync:Add** element (\[MS-ASCMD\] section 2.2.3.7.2) or an **airsync:Change** element (\[MS-ASCMD\] section 2.2.3.24) in the **Sync** command response.

##### Sending E-Mail Changes to the Client

A server SHOULD partition email changes into one or more of the following categories:

-   Changes to the **Read** flag (section [2.2.2.58](#Section_7dce52172fb147508c5216a709aa02cf))

-   Changes to the **Flag** properties (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16))

-   Changes to the **Categories** properties (section [2.2.2.16](#Section_3b761feb2905427e822182ab60becfc6))

-   Changes to other E-Mail class properties, such as **Subject** (section [2.2.2.75.1](#Section_cf3cc4bb765b467195095a16a9184779))

-   Changes to non-E-Mail class properties

If only the **Read** flag has changed for an e-mail item, the server MUST include the **Read** element as the only child element of the **airsync:ApplicationData** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.11) within the **airsync:Change** element (\[MS-ASCMD\] section 2.2.3.24) for that e-mail item in the **Sync** command response.

If only **Flag** properties have changed for an e-mail item, the server MUST include the **Flag** element as the only child element of the **airsync:ApplicationData** element within the **airsync:Change** element for that e-mail item in the **Sync** command response.

If E-Mail class properties of an e-mail item other than the **Read** flag, **Flag**, and **Categories** properties have changed for an e-mail item, the server MUST specify all the e-mail properties as child elements of the **airsync:ApplicationData** element within the **airsync:Change** element for that e-mail item in the **Sync** command response.

If only non-E-Mail class properties of an e-mail item have changed, the server MUST NOT include an **airsync:Change** element for that e-mail item in the **Sync** command response.

The following table specifies what the server MUST return to the client for an e-mail item in the **Sync** command response, based upon which properties have changed for the e-mail item.

  -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Read flag changed   Flag properties changed   Categories properties changed   Other E-Mail class properties changed   Non-E-Mail class properties changed   Server action for e-mail item in Sync command response
  ------------------- ------------------------- ------------------------------- --------------------------------------- ------------------------------------- -------------------------------------------------------------
  No                  No                        No                              No                                      No                                    Send nothing to client

  No                  No                        No                              No                                      Yes                                   Send nothing to client

  No                  No                        No                              Yes                                     No                                    Send full item **airsync:Change** to client

  No                  No                        No                              Yes                                     Yes                                   Send full item **airsync:Change** to client

  No                  No                        Yes                             No                                      No                                    Send **Categories** block only

  No                  No                        Yes                             No                                      Yes                                   Send **Categories** block only

  No                  No                        Yes                             Yes                                     No                                    Send full item **airsync:Change** to client

  No                  No                        Yes                             Yes                                     Yes                                   Send full item **airsync:Change** to client

  No                  Yes                       No                              No                                      No                                    Send **Flag** block only

  No                  Yes                       No                              No                                      Yes                                   Send **Flag** block only

  No                  Yes                       No                              Yes                                     No                                    Send full item **airsync:Change** to client

  No                  Yes                       No                              Yes                                     Yes                                   Send full item **airsync:Change** to client

  No                  Yes                       Yes                             No                                      No                                    Send **Flag** block and **Categories** block

  No                  Yes                       Yes                             No                                      Yes                                   Send **Flag** block and **Categories** block

  No                  Yes                       Yes                             Yes                                     No                                    Send full item **airsync:Change** to client

  No                  Yes                       Yes                             Yes                                     Yes                                   Send full item **airsync:Change** to client

  Yes                 No                        No                              No                                      No                                    Send **Read** flag only

  Yes                 No                        No                              No                                      Yes                                   Send **Read** flag only

  Yes                 No                        No                              Yes                                     No                                    Send full item **airsync:Change** to client

  Yes                 No                        No                              Yes                                     Yes                                   Send full item **airsync:Change** to client

  Yes                 No                        Yes                             No                                      No                                    Send **Read** flag and **Categories** block

  Yes                 No                        Yes                             No                                      Yes                                   Send **Read** flag and **Categories** block

  Yes                 No                        Yes                             Yes                                     No                                    Send full item **airsync:Change** to client

  Yes                 No                        Yes                             Yes                                     Yes                                   Send full item **airsync:Change** to client

  Yes                 Yes                       No                              No                                      No                                    Send **Read** flag and **Flag** block

  Yes                 Yes                       No                              No                                      Yes                                   Send **Read** flag and **Flag** block

  Yes                 Yes                       No                              Yes                                     No                                    Send full item **airsync:Change** to client

  Yes                 Yes                       No                              Yes                                     Yes                                   Send full item **airsync:Change** to client

  Yes                 Yes                       Yes                             No                                      No                                    Send **Read** flag, **Flag** block and **Categories** block

  Yes                 Yes                       Yes                             No                                      Yes                                   Send **Read** flag, **Flag** block and **Categories** block

  Yes                 Yes                       Yes                             Yes                                     No                                    Send full item **airsync:Change** to client

  Yes                 Yes                       Yes                             Yes                                     Yes                                   Send full item **airsync:Change** to client
  -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

##### Updating E-Mail Flags

For every flag update that the client sends to the server in a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21), the server SHOULD update the flag on the e-mail message by using the **Change** element (\[MS-ASCMD\] section 2.2.3.24) of the **Sync** command. The server uses the logic specified in the following table to determine which flag action (clear, set, mark complete) to invoke when updating flag status based on the value of the **Status** element (section [2.2.2.74](#Section_20fba2e8af71412abb1b29c8d0d3fd2d)) in the **Sync** command request.

+--------------------------------------+--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Action                               | Required Properties from Device                                                                                                                                                |
+======================================+================================================================================================================================================================================+
| Flag an email                        | **Status** = 2                                                                                                                                                                 |
|                                      |                                                                                                                                                                                |
|                                      | **FlagType** (section [2.2.2.35](#Section_157c6b73940144e9b9e0fbeadef817f6)) = \"Flag for follow up\"                                                                          |
|                                      |                                                                                                                                                                                |
|                                      | **tasks:StartDate** (section [2.2.2.72](#Section_98e703d64d254727896d49f555465aba)) and **tasks:UtcStartDate** (section [2.2.2.87](#Section_d671fbb9aa284cec8a69089935c504a8)) |
|                                      |                                                                                                                                                                                |
|                                      | **tasks:DueDate** (section [2.2.2.31](#Section_cc1cc4674ecd4ef68ee8d0e52e9da8b3)) and **tasks:UtcDueDate** (section [2.2.2.86](#Section_07e6f69fc7a444a1b0254ec85cd83a0d))     |
|                                      |                                                                                                                                                                                |
|                                      | or                                                                                                                                                                             |
|                                      |                                                                                                                                                                                |
|                                      | **Status** = 2                                                                                                                                                                 |
|                                      |                                                                                                                                                                                |
|                                      | **FlagType** (section 2.2.2.35) = \"Flag for follow up\"                                                                                                                       |
|                                      |                                                                                                                                                                                |
|                                      | **tasks:DateCompleted** (section [2.2.2.23](#Section_c4ce6efa4d0040af9bbf1582186dcf59))                                                                                        |
|                                      |                                                                                                                                                                                |
|                                      | **CompleteTime** (section [2.2.2.19](#Section_79799c317acb46fcb4c4fdbac218c383))                                                                                               |
+--------------------------------------+--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Flag a task                          | **Status** = 2                                                                                                                                                                 |
|                                      |                                                                                                                                                                                |
|                                      | **tasks:Subject** (section [2.2.2.75.2](#Section_6163992744994a5484d7935a9b591f56)) = user defined                                                                             |
|                                      |                                                                                                                                                                                |
|                                      | **FlagType** = \"Flag for follow up\"                                                                                                                                          |
|                                      |                                                                                                                                                                                |
|                                      | **tasks:StartDate** and **tasks:UtcStartDate**                                                                                                                                 |
|                                      |                                                                                                                                                                                |
|                                      | **tasks:DueDate** and **tasks:UtcDueDate**                                                                                                                                     |
|                                      |                                                                                                                                                                                |
|                                      | **tasks:ReminderSet** (section [2.2.2.64](#Section_e19643a39288466eb691ef98ef60df1a))                                                                                          |
|                                      |                                                                                                                                                                                |
|                                      | **tasks:ReminderTime** (section [2.2.2.65](#Section_6e3300c9da1b4f9aad4e95f131fdb140))                                                                                         |
|                                      |                                                                                                                                                                                |
|                                      | or                                                                                                                                                                             |
|                                      |                                                                                                                                                                                |
|                                      | **Status** = 2                                                                                                                                                                 |
|                                      |                                                                                                                                                                                |
|                                      | **tasks:DateCompleted**                                                                                                                                                        |
|                                      |                                                                                                                                                                                |
|                                      | **CompleteTime** (section 2.2.2.19)                                                                                                                                            |
+--------------------------------------+--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Mark an email flag as complete       | **Status** = 1                                                                                                                                                                 |
|                                      |                                                                                                                                                                                |
|                                      | **CompleteTime** (section 2.2.2.19)                                                                                                                                            |
|                                      |                                                                                                                                                                                |
|                                      | **tasks:DateCompleted**                                                                                                                                                        |
+--------------------------------------+--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Mark a task flag as complete         | **Status** = 1                                                                                                                                                                 |
|                                      |                                                                                                                                                                                |
|                                      | **CompleteTime**                                                                                                                                                               |
|                                      |                                                                                                                                                                                |
|                                      | **tasks:DateCompleted**                                                                                                                                                        |
+--------------------------------------+--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Clearing the flag on an email        | **Status** = 0 or                                                                                                                                                              |
|                                      |                                                                                                                                                                                |
|                                      | **Flag** node empty                                                                                                                                                            |
+--------------------------------------+--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Clearing the flag on a task          | **Status** = 0 or                                                                                                                                                              |
|                                      |                                                                                                                                                                                |
|                                      | **Flag** node empty                                                                                                                                                            |
+--------------------------------------+--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Update the flag metadata on an email | All updated properties                                                                                                                                                         |
+--------------------------------------+--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Update flag metadata on a task       | All updated properties                                                                                                                                                         |
+--------------------------------------+--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+

The **Sync** command response includes an **airsync:Status** element (\[MS-ASCMD\] section 2.2.3.177.17) value of 6 if any of the required elements listed in the table are missing from the **Sync** command request.

### Timer Events

None.

### Other Local Events

None.

# Protocol Examples

The examples in this section use decoded values of the [**Uniform Resource Identifier (URI)**](#gt_e18af8e8-01d7-4f91-8a1e-0fb21b191f95) query parameters and the message body for clarity. The URI query parameter is encoded with [**base64 encoding**](#gt_179b9392-9019-45a3-880b-26f6890522b7) and the body is [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc)-encoded when sent across the wire. For more information about the base64 encoding used in the URI query parameter, see [\[MS-ASHTTP\]](%5bMS-ASHTTP%5d.pdf#Section_4cbf28dc287641c69d87ba9db86cd40d) section 2.2.1.1.1. For more information about WBXML encoding, see [\[MS-ASWBXML\]](%5bMS-ASWBXML%5d.pdf#Section_39973eb11e404eb5ac7442781c5a33bc).

## Synchronizing E-Mail

### Synchronizing Only E-Mail Metadata

The following example demonstrates a client request to synchronize e-mail metadata in the [**Inbox folder**](#gt_baa08600-0402-47f6-a8ce-9690cf962c96) with the server, and the server response. The **CollectionId** element ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.3.30.6) in the **Sync** command request (\[MS-ASCMD\] section 2.2.1.21) identifies the Inbox folder as the folder to synchronize, and because the request does not specify that the response include e-mail body content, only e-mail metadata is included in the response. In this example, the server returns metadata for one e-mail item. The **Sync** command response includes the estimated size and body type of the e-mail message, but does not include the body of the message.

Request:

30. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

    \<Sync xmlns=\"AirSync:\"\>

    \<Collections\>

    \<Collection\>

    \<SyncKey\>927479200\</SyncKey\>

    \<CollectionId\>5\</CollectionId\>

    \<DeletesAsMoves\>1\</DeletesAsMoves\>

    \<GetChanges\>1\</GetChanges\>

    \<WindowSize\>512\</WindowSize\>

    \</Collection\>

    \</Collections\>

    \</Sync\>

Response:

42. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

    \<Sync xmlns:email=\"Email:\" xmlns:airsyncbase=\"AirSyncBase:\" xmlns:email2=\"Email2:\" xmlns=\"AirSync:\"\>

    \<Collections\>

    \<Collection\>

    \<SyncKey\>927479210\</SyncKey\>

    \<CollectionId\>5\</CollectionId\>

    \<Status\>1\</Status\>

    \<Commands\>

    \<Add\>

    \<ServerId\>5:1\</ServerId\>

    \<ApplicationData\>

    \<email:To\>\"Device User\" &lt;someone1@example.com&gt;\</email:To\>

    \<email:From\>\"Device User 2\" &lt;someone2@example.com&gt;\</email:From\>

    \<email:Subject\>New mail message\</email:Subject\>

    \<email:DateReceived\>2009-07-29T19:25:37.817Z\</email:DateReceived\>

    \<email:DisplayTo\>Device User\</email:DisplayTo\>

    \<email:ThreadTopic\>New mail message\</email:ThreadTopic\>

    \<email:Importance\>1\</email:Importance\>

    \<email:Read\>0\</email:Read\>

    \<airsyncbase:Body\>

    \<airsyncbase:Type\>2\</airsyncbase:Type\>

    \<airsyncbase:EstimatedDataSize\>116575\</airsyncbase:EstimatedDataSize\>

    \<airsyncbase:Truncated\>1\</airsyncbase:Truncated\>

    \</airsyncbase:Body\>

    \<email:MessageClass\>IPM.Note\</email:MessageClass\>

    \<email:InternetCPID\>1252\</email:InternetCPID\>

    \<email:Flag /\>

    \<email:ContentClass\>urn:content-classes:message\</email:ContentClass\>

    \<airsyncbase:NativeBodyType\>2\</airsyncbase:NativeBodyType\>

    \<email2:ConversationId\>FF68022058BD485996BE15F6F6D99320\</email2:ConversationId\>

    \<email2:ConversationIndex\>CA2CFA8A23\</email2:ConversationIndex\>

    \<email:Categories /\>

    \</ApplicationData\>

    \</Add\>

    \</Commands\>

    \</Collection\>

    \</Collections\>

    \</Sync\>

### Synchronizing E-Mail Metadata and Body

The following example demonstrates a client request to synchronize both e-mail metadata and body with the server, and the server response. The **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) includes the **airsyncbase:BodyPreference** element ([\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.12) to request that the server return e-mail body in [**Hypertext Markup Language (HTML)**](#gt_549c4960-e8be-4c24-bc2b-b86530f1c1bf) format (**airsyncbase:Type** element (\[MS-ASAIRS\] section 2.2.2.41.4) value is 2) with each e-mail body truncated to 5,120 bytes (**airsyncbase:TruncationSize** element (\[MS-ASAIRS\] section 2.2.2.40.2) value is 5120). In this example, the **Sync** command response includes the metadata and body for one HTML e-mail message.

**Note  **Although not shown in this example, **Sync** command requests can include multiple **airsyncbase:BodyPreference** elements to specify different **airsyncbase:TruncationSize** values for each **airsyncbase:Type** value.

Request:

80. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

    \<Sync xmlns:AirSyncBase=\"airsyncbase:\" xmlns=\"AirSync:\"\>

    \<Collections\>

    \<Collection\>

    \<SyncKey\>1534587728\</SyncKey\>

    \<CollectionId\>5\</CollectionId\>

    \<DeletesAsMoves\>1\</DeletesAsMoves\>

    \<GetChanges\>1\</GetChanges\>

    \<WindowSize\>512\</WindowSize\>

    \<Options\>

    \<MIMESupport\>0\</MIMESupport\>

    \<airsyncbase:BodyPreference\>

    \<airsyncbase:Type\>2\</airsyncbase:Type\>

    \<airsyncbase:TruncationSize\>5120\</airsyncbase:TruncationSize\>

    \</airsyncbase:BodyPreference\>

    \</Options\>

    \</Collection\>

    \</Collections\>

    \</Sync\>

Response:

99. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

    \<Sync xmlns:email=\"Email:\" xmlns:email2=\"Email2\" xmlns:airsyncbase=\"AirSyncBase:\" xmlns=\"AirSync:\"\>

    \<Collections\>

    \<Collection\>

    \<SyncKey\>1174511196\</SyncKey\>

    \<CollectionId\>5\</CollectionId\>

    \<Status\>1\</Status\>

    \<Commands\>

    \<Add\>

    \<ServerId\>5:10\</ServerId\>

    \<ApplicationData\>

    \<email:To\>\"Device User\" &lt;someone1@example.com&gt;\</email:To\>

    \<email:From\>\"Device User2\" &lt;someone2@example.com&gt;\</email:From\>

    \<email:Subject\>Sample HTML message\</email:Subject\>

    \<email:DateReceived\>2009-02-19T01:43:25.266Z\</email:DateReceived\>

    \<email:DisplayTo\>Device User\</email:DisplayTo\>

    \<email:ThreadTopic\>Sample HTML message\</email:ThreadTopic\>

    \<email:Importance\>1\</email:Importance\>

    \<email:Read\>0\</email:Read\>

    \<airsyncbase:Body\>

    \<airsyncbase:Type\>2\</airsyncbase:Type\>

    \<airsyncbase:EstimatedDataSize\>375\</airsyncbase:EstimatedDataSize\>

    \<airsyncbase:Data\>&lt;html dir=\"ltr\"&gt;

    &lt;head&gt;

    &lt;meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"&gt;

    &lt;style id=\"owaParaStyle\"&gt;

    &lt;!\--

    p

    {margin-top:0px;

    margin-bottom:0px}

    \--&gt;

    &lt;/style&gt;

    &lt;/head&gt;

    &lt;body&gt;

    &lt;div style=\"font-size:13px; color:#000000; direction:ltr; font-family:Tahoma\"&gt;

    &lt;div&gt;This is&amp;nbsp;the body of an&amp;nbsp;HTML e-mail message.&lt;/div&gt;

    &lt;/div&gt;

    &lt;/body&gt;

    &lt;/html&gt;

    \</airsyncbase:Data\>

    \</airsyncbase:Body\>

    \<email:MessageClass\>IPM.Note\</email:MessageClass\>

    \<email:InternetCPID\>28591\</email:InternetCPID\>

    \<email:Flag /\>

    \<email:ContentClass\>urn:content-classes:message\</email:ContentClass\>

    \<airsyncbase:NativeBodyType\>2\</airsyncbase:NativeBodyType\>

    \<email2:ConversationId\>FF68022058BD485996BE15F6F6D99320\</email2:ConversationId\>

    \<email2:ConversationIndex\>CA2CFA8A23\</email2:ConversationIndex\>

    \<email:Categories /\>

    \</ApplicationData\>

    \</Add\>

    \<Change\>

    \<ServerId\>5:8\</ServerId\>

    \<ApplicationData\>

    \<email:Read\>1\</email:Read\>

    \</ApplicationData\>

    \</Change\>

    \</Commands\>

    \</Collection\>

    \</Collections\>

    \</Sync\>

### Synchronizing E-Mail Attachments

Each example in this section demonstrates a **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) that contains an attachment.

#### Synchronizing an E-Mail with an Electronic Voice Mail Attachment

The following example shows a **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) returned by the server to the client that contains one e-mail item with an electronic voice mail attachment. The e-mail item data is contained within an **Add** element (\[MS-ASCMD\] section 2.2.3.7.2), conveying to the client that the e-mail item (with attachment) needs to be created on the client.

160. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<Sync xmlns=\"AirSync:\" xmlns:email=\"Email:\" xmlns:airsyncbase=\"AirSyncBase:\" xmlns:email2=\"Email2:\"\>

     \<Collections\>

     \<Collection\>

     \<SyncKey\>1336143213\</SyncKey\>

     \<CollectionId\>20\</CollectionId\>

     \<Status\>1\</Status\>

     \<Commands\>

     \<Add\>

     \<ServerId\>20:2\</ServerId\>

     \<ApplicationData\>

     \<email:To\>\"Device User\" &lt;someone@example.com&gt;\</email:To\>

     \<email:From\>\"7125550123\" &lt;7125550123&gt;\</email:From\>

     \<email:Subject\>Voice Mail from 7125550123 (3 seconds)\</email:Subject\>

     \<email:DateReceived\>2007-11-06T23:42:16.829Z\</email:DateReceived\>

     \<email:DisplayTo\>Device User\</email:DisplayTo\>

     \<email:ThreadTopic\>Voice Mail from 7125550123 (3 seconds)\</email:ThreadTopic\>

     \<email:Importance\>1\</email:Importance\>

     \<email:Read\>1\</email:Read\>

     \<airsyncbase:Attachments\>

     \<airsyncbase:Attachment\>

     \<airsyncbase:DisplayName\>7125550123 (3 seconds) Voice Mail.wma\</airsyncbase:DisplayName\>

     \<airsyncbase:FileReference\>20%3a2%3a0\</airsyncbase:FileReference\>

     \<airsyncbase:Method\>1\</airsyncbase:Method\>

     \<airsyncbase:EstimatedDataSize\>9025\</airsyncbase:EstimatedDataSize\>

     \<email2:UmAttOrder\>1\</email2:UmAttOrder\>

     \<email2:UmAttDuration\>3\</email2:UmAttDuration\>

     \</airsyncbase:Attachment\>

     \</airsyncbase:Attachments\>

     \<airsyncbase:Body\>

     \<airsyncbase:Type\>3\</airsyncbase:Type\>

     \<airsyncbase:EstimatedDataSize\>1512\</airsyncbase:EstimatedDataSize\>

     \<airsyncbase:Truncated\>1\</airsyncbase:Truncated\>

     \</airsyncbase:Body\>

     \<email:MessageClass\>IPM.Note.Microsoft.Voicemail.UM.CA\</email:MessageClass\>

     \<email:InternetCPID\>20127\</email:InternetCPID\>

     \<email:ContentClass\>urn:content-classes:message\</email:ContentClass\>

     \<airsyncbase:NativeBodyType\>3\</airsyncbase:NativeBodyType\>

     \<email2:CallerID\>7125550123\</email2:CallerID\>

     \<email2:UmUserNotes\>7125550123\</email2:UmUserNotes\>

     \</ApplicationData\>

     \</Add\>

     \</Commands\>

     \</Collection\>

     \</Collections\>

     \</Sync\>

#### Synchronizing an E-mail with a Text Attachment

The following example shows the **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) returned by the server to the client that contains one e-mail item with a text attachment. The e-mail item data is contained within an **Add** element (\[MS-ASCMD\] section 2.2.3.7.2), conveying to the client that the e-mail item (with attachment) needs to be created on the client.

206. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<Sync xmlns:email=\"Email:\" xmlns:airsyncbase=\"AirSyncBase:\" xmlns:email2=\"Email2:\" xmlns=\"AirSync:\"\>

     \<Collections\>

     \<Collection\>

     \<SyncKey\>334239291\</SyncKey\>

     \<CollectionId\>5\</CollectionId\>

     \<Status\>1\</Status\>

     \<Commands\>

     \<Add\>

     \<ServerId\>5:3\</ServerId\>

     \<ApplicationData\>

     \<email:To\>\"Device User\" &lt;someone1@example.com&gt;\</email:To\>

     \<email:From\>\"Device User2\" &lt;someone2@example.com&gt;\</email:From\>

     \<email:Subject\>With Attachment\</email:Subject\>

     \<email:DateReceived\>2009-03-04T22:48:41.211Z\</email:DateReceived\>

     \<email:DisplayTo\>Device User\</email:DisplayTo\>

     \<email:ThreadTopic\>With Attachment\</email:ThreadTopic\>

     \<email:Importance\>1\</email:Importance\>

     \<email:Read\>0\</email:Read\>

     \<airsyncbase:Attachments\>

     \<airsyncbase:Attachment\>

     \<airsyncbase:DisplayName\>Test.txt\</airsyncbase:DisplayName\>

     \<airsyncbase:FileReference\>5%3a3%3a0\</airsyncbase:FileReference\>

     \<airsyncbase:Method\>1\</airsyncbase:Method\>

     \<airsyncbase:EstimatedDataSize\>84\</airsyncbase:EstimatedDataSize\>

     \</airsyncbase:Attachment\>

     \</airsyncbase:Attachments\>

     \<airsyncbase:Body\>

     \<airsyncbase:Type\>1\</airsyncbase:Type\>

     \<airsyncbase:EstimatedDataSize\>33\</airsyncbase:EstimatedDataSize\>

     \<airsyncbase:Truncated\>1\</airsyncbase:Truncated\>

     \</airsyncbase:Body\>

     \<email:MessageClass\>IPM.Note\</email:MessageClass\>

     \<email:InternetCPID\>20127\</email:InternetCPID\>

     \<email:Flag /\>

     \<email:ContentClass\>urn:content-classes:message\</email:ContentClass\>

     \<airsyncbase:NativeBodyType\>1\</airsyncbase:NativeBodyType\>

     \<email2:ConversationId\>¥gÈtent-cl\<email2:ConversationId\>

     \<email2:ConversationIndex\>...\<email2:ConversationIndex\>

     \<email:Categories /\>

     \</ApplicationData\>

     \</Add\>

     \</Commands\>

     \</Collection\>

     \</Collections\>

     \</Sync\>

### Deleting an E-Mail

The following example demonstrates a client request to delete an e-mail from the server, and the server response. In this example, the **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) specifies that the server delete the e-mail message that has a **ServerId** (\[MS-ASCMD\] section 2.2.3.166.8) value of 5:10 and the **Sync** command response indicates that the e-mail was deleted successfully by returning a **Status** element (\[MS-ASCMD\] section 2.2.3.177.17) value of 1.

Request:

252. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<Sync xmlns=\"AirSync:\"\>

     \<Collections\>

     \<Collection\>

     \<SyncKey\>1174511196\</SyncKey\>

     \<CollectionId\>5\</CollectionId\>

     \<DeletesAsMoves\>1\</DeletesAsMoves\>

     \<GetChanges\>1\</GetChanges\>

     \<WindowSize\>512\</WindowSize\>

     \<Commands\>

     \<Delete\>

     \<ServerId\>5:10\</ServerId\>

     \</Delete\>

     \</Commands\>

     \</Collection\>

     \</Collections\>

     \</Sync\>

Response:

269. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<Sync xmlns=\"AirSync:\"\>

     \<Collections\>

     \<Collection\>

     \<SyncKey\>721953595\</SyncKey\>

     \<CollectionId\>5\</CollectionId\>

     \<Status\>1\</Status\>

     \</Collection\>

     \</Collections\>

     \</Sync\>

### Synchronizing Meeting Requests

Each example in this section demonstrates a **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) that contains a meeting request.

#### Synchronizing a Non-Recurring Meeting Request

The following example shows a **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) returned by the server to the client that contains one meeting request for a non-recurring meeting. The meeting request data is contained within an **Add** element (\[MS-ASCMD\] section 2.2.3.7.2), conveying to the client that the meeting request needs to be created on the client.

279. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<Sync xmlns:email=\"Email:\" xmlns:airsyncbase=\"AirSyncBase:\" xmlns:email2=\"Email2:\" xmlns=\"AirSync:\"\>

     \<Collections\>

     \<Collection\>

     \<SyncKey\>1419832287\</SyncKey\>

     \<CollectionId\>5\</CollectionId\>

     \<Status\>1\</Status\>

     \<Commands\>

     \<Add\>

     \<ServerId\>5:13\</ServerId\>

     \<ApplicationData\>

     \<email:To\>\"Device User\" &lt;someone1@example.com&gt;\</email:To\>

     \<email:From\>\"Device User2\" &lt;someone2@example.com&gt;\</email:From\>

     \<email:Subject\>Example Meeting Request\</email:Subject\>

     \<email:DateReceived\>2009-02-19T08:35:17.922Z\</email:DateReceived\>

     \<email:DisplayTo\>Device User\</email:DisplayTo\>

     \<email:ThreadTopic\>Example Meeting Request\</email:ThreadTopic\>

     \<email:Importance\>1\</email:Importance\>

     \<email:Read\>0\</email:Read\>

     \<airsyncbase:Body\>

     \<airsyncbase:Type\>3\</airsyncbase:Type\>

     \<airsyncbase:EstimatedDataSize\>437\</airsyncbase:EstimatedDataSize\>

     \<airsyncbase:Truncated\>1\</airsyncbase:Truncated\>

     \</airsyncbase:Body\>

     \<email:MessageClass\>IPM.Schedule.Meeting.Request\</email:MessageClass\>

     \<email:MeetingRequest\>

     \<email:AllDayEvent\>0\</email:AllDayEvent\>

     \<email:StartTime\>2009-02-20T15:30:00.000Z\</email:StartTime\>

     \<email:DtStamp\>2009-02-19T08:35:15.786Z\</email:DtStamp\>

     \<email:EndTime\>2009-02-20T16:30:00.000Z\</email:EndTime\>

     \<email:InstanceType\>0\</email:InstanceType\>

     \<email:Location\>Cafe\</email:Location\>

     \<email:Organizer\>\"Device User2\" &lt;someone2@example.com&gt;\</email:Organizer\>

     \<email:Reminder\>900\</email:Reminder\>

     \<email:ResponseRequested\>1\</email:ResponseRequested\>

     \<email:Sensitivity\>0\</email:Sensitivity\>

     \<email:BusyStatus\>2\</email:BusyStatus\>

     \<email:TimeZone\>aAEAACgARwBNAFQALQAwADYAOgAwADAAKQAgAEMAZQBuAHQAcgBhAGwAIABUAGkAbQBlACAAKABVAFMAIAAmACAAQwAAAAsAAAABAAIAAAAAAAAAAAAAACgARwBNAFQALQAwADYAOgAwADAAKQAgAEMAZQBuAHQAcgBhAGwAIABUAGkAbQBlACAAKABVAFMAIAAmACAAQwAAAAMAAAACAAIAAAAAAAAAxP///w==\</email:TimeZone\>

     \<email:GlobalObjId\>BAAAAIIA4AB0xbcQGoLgCAAAAADYSxf9bJLJAQAAAAAAAAAAEAAAAJEHL7SUox5GtgQV1TYDY4A=\</email:GlobalObjId\>

     \</email:MeetingRequest\>

     \<email:InternetCPID\>28591\</email:InternetCPID\>

     \<email:Flag /\>

     \<email:ContentClass\>urn:content-classes:calendarmessage\</email:ContentClass\>

     \<airsyncbase:NativeBodyType\>3\</airsyncbase:NativeBodyType\>

     \<email2:ConversationId\>LðØ‡\*û@à²&#x1A;&#x15;EñMØ±\</email2:ConversationId\>

     \<email2:ConversationIndex\>É\'lý&#x1F;\</email2:ConversationIndex\>

     \<email:Categories /\>

     \</ApplicationData\>

     \</Add\>

     \</Commands\>

     \</Collection\>

     \</Collections\>

     \</Sync\>

#### Synchronizing a Recurring Meeting Request

The following example shows a **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) that is returned by the server to the client and contains one meeting request for a recurring meeting. The meeting occurs every month (**Type** element (section [2.2.2.80](#Section_df2f263ad570471883d080f2b5a2576e)) value is 3 and **Interval** element (section [2.2.2.41](#Section_fe46604894324a86917498c1943c2a2f)) value is 1), during the third week of the month (**WeekOfMonth** element (section [2.2.2.88](#Section_08abd347d9734a1cbecbdde8679ebc78)) value is 3) on Tuesday (**DayOfWeek** element (section [2.2.2.26](#Section_9dc2f54241ef46598b0591ab96c8cb33)) value is 4). The meeting request data is contained within an **Add** element (\[MS-ASCMD\] section 2.2.3.7.2), conveying to the client that the meeting request needs to be created on the client.

332. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<Sync xmlns:email=\"Email:\" xmlns:airsyncbase=\"AirSyncBase:\" xmlns:email2=\"Email2:\" xmlns=\"AirSync:\"\>

     \<Collections\>

     \<Collection\>

     \<SyncKey\>2086787787\</SyncKey\>

     \<CollectionId\>5\</CollectionId\>

     \<Status\>1\</Status\>

     \<Commands\>

     \<Add\>

     \<ServerId\>5:14\</ServerId\>

     \<ApplicationData\>

     \<email:To\>\"Device User\" &lt;someone1@example.com&gt;\</email:To\>

     \<email:From\>\"Device User2\" &lt;someone2@example.com&gt;\</email:From\>

     \<email:Subject\>Monthly Meeting\</email:Subject\>

     \<email:DateReceived\>2009-02-19T08:47:21.842Z\</email:DateReceived\>

     \<email:DisplayTo\>Device User\</email:DisplayTo\>

     \<email:ThreadTopic\>Monthly Meeting\</email:ThreadTopic\>

     \<email:Importance\>1\</email:Importance\>

     \<email:Read\>0\</email:Read\>

     \<airsyncbase:Body\>

     \<airsyncbase:Type\>3\</airsyncbase:Type\>

     \<airsyncbase:EstimatedDataSize\>503\</airsyncbase:EstimatedDataSize\>

     \<airsyncbase:Truncated\>1\</airsyncbase:Truncated\>

     \</airsyncbase:Body\>

     \<email:MessageClass\>IPM.Schedule.Meeting.Request\</email:MessageClass\>

     \<email:MeetingRequest\>

     \<email:AllDayEvent\>0\</email:AllDayEvent\>

     \<email:StartTime\>2009-03-17T20:00:00.000Z\</email:StartTime\>

     \<email:DtStamp\>2009-02-19T08:47:19.527Z\</email:DtStamp\>

     \<email:EndTime\>2009-03-17T21:00:00.000Z\</email:EndTime\>

     \<email:InstanceType\>1\</email:InstanceType\>

     \<email:Location\>My Office\</email:Location\>

     \<email:Organizer\>\"Device User2\" &lt;someone2@example.com&gt;\</email:Organizer\>

     \<email:Reminder\>900\</email:Reminder\>

     \<email:ResponseRequested\>1\</email:ResponseRequested\>

     \<email:Recurrences\>

     \<email:Recurrence\>

     \<email:Type\>3\</email:Type\>

     \<email:Interval\>1\</email:Interval\>

     \<email:Until\>20091229T210000Z\</email:Until\>

     \<email:WeekOfMonth\>3\</email:WeekOfMonth\>

     \<email:DayOfWeek\>4\</email:DayOfWeek\>

     \</email:Recurrence\>

     \</email:Recurrences\>

     \<email:Sensitivity\>0\</email:Sensitivity\>

     \<email:BusyStatus\>2\</email:BusyStatus\>

     \<email:TimeZone\>aAEAACgARwBNAFQALQAwADYAOgAwADAAKQAgAEMAZQBuAHQAcgBhAGwAIABUAGkAbQBlACAAKABVAFMAIAAmACAAQwAAAAsAAAABAAIAAAAAAAAAAAAAACgARwBNAFQALQAwADYAOgAwADAAKQAgAEMAZQBuAHQAcgBhAGwAIABUAGkAbQBlACAAKABVAFMAIAAmACAAQwAAAAMAAAACAAIAAAAAAAAAxP///w==\</email:TimeZone\>

     \<email:GlobalObjId\>BAAAAIIA4AB0xbcQGoLgCAAAAADok5WnbpLJAQAAAAAAAAAAEAAAAP4Ao5IYwQdKiFkDBeGTtgY=\</email:GlobalObjId\>

     \</email:MeetingRequest\>

     \<email:InternetCPID\>28591\</email:InternetCPID\>

     \<email:Flag /\>

     \<email:ContentClass\>urn:content-classes:calendarmessage\</email:ContentClass\>

     \<airsyncbase:NativeBodyType\>3\</airsyncbase:NativeBodyType\>

     \<email2:ConversationId\>\'MÅ□\'&amp;Kä°V÷ŽÓ&#x16;xû\</email2:ConversationId\>

     \<email2:ConversationIndex\>É\'n¬„\</email2:ConversationIndex\>

     \<email:Categories /\>

     \</ApplicationData\>

     \</Add\>

     \</Commands\>

     \</Collection\>

     \</Collections\>

     \</Sync\>

### Retrieving E-Mail Metadata and Body

The following example demonstrates a client request to retrieve the metadata and body of a specific e-mail, and the server response. In the **ItemOperations** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.10), the e-mail to be retrieved is identified by the **ServerId** element (\[MS-ASCMD\] section 2.2.3.166.7) value. In this example, the contents of the **airsyncbase:Data** element ([\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.20.1) in the **ItemOperations** command response is [**XML**](#gt_982b7f8e-d516-4fd5-8d5e-1a836081ed85)-escaped (that is, not [**HTML**](#gt_549c4960-e8be-4c24-bc2b-b86530f1c1bf)). However, as these values are passed over the wire by using [**WBXML**](#gt_46afe83a-7afd-42b3-8e27-07b6ae8d3dbc), they are passed unencoded (that is, the contents of the **airsyncbase:Data** element contains the characters \"\<\" and \"\>\").

Request:

394. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<ItemOperations xmlns:airsync=\"AirSync:\" xmlns:airsyncbase=\"AirSyncBase:\" xmlns=\"ItemOperations:\"\>

     \<Fetch\>

     \<Store\>Mailbox\</Store\>

     \<airsync:CollectionId\>5\</airsync:CollectionId\>

     \<airsync:ServerId\>5:10\</airsync:ServerId\>

     \<Options\>

     \<airsyncbase:BodyPreference\>

     \<airsyncbase:Type\>2\</airsyncbase:Type\>

     \</airsyncbase:BodyPreference\>

     \</Options\>

     \</Fetch\>

     \</ItemOperations\>

Response:

407. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<ItemOperations xmlns:airsync=\"AirSync:\" xmlns:email=\"Email:\" xmlns:airsyncbase=\"AirSyncBase:\" xmlns:email2=\"Email2:\" xmlns=\"ItemOperations:\"\>

     \<Status\>1\</Status\>

     \<Response\>

     \<Fetch\>

     \<Status\>1\</Status\>

     \<airsync:CollectionId\>5\</airsync:CollectionId\>

     \<airsync:ServerId\>5:10\</airsync:ServerId\>

     \<airsync:Class\>Email\</airsync:Class\>

     \<Properties\>

     \<email:To\>\"Device User\" &lt;someone1@example.com&gt;\</email:To\>

     \<email:From\>\"Device User2\" &lt;someone2@example.com&gt;\</email:From\>

     \<email:Subject\>Fetch this content.\</email:Subject\>

     \<email:DateReceived\>2009-02-19T01:43:25.266Z\</email:DateReceived\>

     \<email:DisplayTo\>Device User\</email:DisplayTo\>

     \<email:ThreadTopic\>Fetch this content.\</email:ThreadTopic\>

     \<email:Importance\>1\</email:Importance\>

     \<email:Read\>0\</email:Read\>

     \<airsyncbase:Body\>

     \<airsyncbase:Type\>2\</airsyncbase:Type\>

     \<airsyncbase:EstimatedDataSize\>376\</airsyncbase:EstimatedDataSize\>

     \<airsyncbase:Data\>&lt;html dir=\"ltr\"&gt;

     &lt;head&gt;

     &lt;meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"&gt;

     &lt;style&gt;&lt;/style&gt;&lt;style id=\"owaParaStyle\"&gt;

     &lt;!\--

     p

     {margin-top:0px;

     margin-bottom:0px}

     \--&gt;

     &lt;/style&gt;

     &lt;/head&gt;

     &lt;body&gt;

     &lt;div style=\"font-size:13px; color:#000000; direction:ltr; font-family:Tahoma\"&gt;

     &lt;div&gt;This is the content that was truncated.&lt;/div&gt;

     &lt;/div&gt;

     &lt;/body&gt;

     &lt;/html&gt;

     \</airsyncbase:Data\>

     \</airsyncbase:Body\>

     \<email:MessageClass\>IPM.Note\</email:MessageClass\>

     \<email:InternetCPID\>28591\</email:InternetCPID\>

     \<email:Flag /\>

     \<email:ContentClass\>urn:content-classes:message\</email:ContentClass\>

     \<airsyncbase:NativeBodyType\>2\</airsyncbase:NativeBodyType\>

     \<email2:ConversationId\>€%ÿ&#x18;&#x8;:B˜\</email2:ConversationId\>

     \<email2:ConversationIndex\>&#x18;&#x8\</email2:ConversationIndex\>

     \</Properties\>

     \</Fetch\>

     \</Response\>

     \</ItemOperations\>

## Setting the Flag on an E-Mail

The examples in this section show how to use the **Sync** command request and **Sync** command response ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) to set e-mail flags on the client and the server.

Note the following:

-   Implicit deletes: If an element is not present within the **Flag** container element (section [2.2.2.34](#Section_af040c59b10c4b6ebd0c393705130a16)) in a request or response, then the corresponding property is deleted.

-   Although elements from the **Tasks** namespace do appear in the following examples, all properties are saved on the e-mail item only. No task items are created.

### Setting a Flag

The following example shows a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) sent from the client to the server to set a flag with a start date and due date, but with no reminder.

458. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<Sync xmlns:email=\"Email:\" xmlns:tasks=\"Tasks:\" xmlns=\"AirSync:\"\>

     \<Collections\>

     \<Collection\>

     \<SyncKey\>1520171944\</SyncKey\>

     \<CollectionId\>5\</CollectionId\>

     \<DeletesAsMoves\>1\</DeletesAsMoves\>

     \<GetChanges\>1\</GetChanges\>

     \<WindowSize\>512\</WindowSize\>

     \<Commands\>

     \<Change\>

     \<ServerId\>5:3\</ServerId\>

     \<ApplicationData\>

     \<email:Read\>1\</email:Read\>

     \<email:Flag\>

     \<email:Status\>2\</email:Status\>

     \<email:FlagType\>for Follow Up\</email:FlagType\>

     \<tasks:StartDate\>2009-02-24T08:00:00.000Z\</tasks:StartDate\>

     \<tasks:UtcStartDate\>2009-02-24T08:00:00.000Z\</tasks:UtcStartDate\>

     \<tasks:DueDate\>2009-02-25T12:00:00.000Z\</tasks:DueDate\>

     \<tasks:UtcDueDate\>2009-02-25T12:00:00.000Z\</tasks:UtcDueDate\>

     \<tasks:ReminderSet\>0\</tasks:ReminderSet\>

     \</email:Flag\>

     \</ApplicationData\>

     \</Change\>

     \</Commands\>

     \</Collection\>

     \</Collections\>

     \</Sync\>

The following example shows an independent **Sync** command response sent from the server to the client to set a flag with a start date, a due date, and a reminder on the client.

487. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<Sync xmlns:email=\"Email:\" xmlns:tasks=\"Tasks:\" xmlns=\"AirSync:\"\>

     \<Collections\>

     \<Collection\>

     \<SyncKey\>735431712\</SyncKey\>

     \<CollectionId\>5\</CollectionId\>

     \<Status\>1\</Status\>

     \<Commands\>

     \<Change\>

     \<ServerId\>5:7\</ServerId\>

     \<ApplicationData\>

     \<email:Flag\>

     \<tasks:DueDate\>2009-02-20T08:00:00.000Z\</tasks:DueDate\>

     \<tasks:UtcDueDate\>2009-02-20T08:00:00.000Z\</tasks:UtcDueDate\>

     \<tasks:UtcStartDate\>2009-02-19T08:00:00.000Z\</tasks:UtcStartDate\>

     \<tasks:Subject\>With Reminder\</tasks:Subject\>

     \<email:Status\>2\</email:Status\>

     \<email:FlagType\>Flag for follow up\</email:FlagType\>

     \<tasks:StartDate\>2009-02-19T08:00:00.000Z\</tasks:StartDate\>

     \<tasks:ReminderSet\>1\</tasks:ReminderSet\>

     \<tasks:ReminderTime\>2009-02-19T21:00:00.000Z\</tasks:ReminderTime\>

     \</email:Flag\>

     \</ApplicationData\>

     \</Change\>

     \</Commands\>

     \</Collection\>

     \</Collections\>

     \</Sync\>

### Marking a Flag as Complete

The following example shows a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) sent from the client to the server to mark a flag as complete. The **Status** element (section [2.2.2.74](#Section_20fba2e8af71412abb1b29c8d0d3fd2d)) value of 1 indicates that the flag status is complete. The **tasks:DateCompleted** element (section [2.2.2.23](#Section_c4ce6efa4d0040af9bbf1582186dcf59)) value indicates when the user updated the e-mail flag in the client to mark it as complete, and the **CompleteTime** element (section [2.2.2.19](#Section_79799c317acb46fcb4c4fdbac218c383)) value indicates the time that the item was marked as finished.

515. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<Sync xmlns:email=\"Email:\" xmlns:tasks=\"Tasks:\" xmlns=\"AirSync:\"\>

     \<Collections\>

     \<Collection\>

     \<SyncKey\>509846121\</SyncKey\>

     \<CollectionId\>5\</CollectionId\>

     \<DeletesAsMoves\>1\</DeletesAsMoves\>

     \<GetChanges\>1\</GetChanges\>

     \<WindowSize\>512\</WindowSize\>

     \<Commands\>

     \<Change\>

     \<ServerId\>5:5\</ServerId\>

     \<ApplicationData\>

     \<email:Read\>1\</email:Read\>

     \<email:Flag\>

     \<email:Status\>1\</email:Status\>

     \<email:FlagType\>Flag for follow up\</email:FlagType\>

     \<email:CompleteTime\>2009-02-19T08:30:00.000Z\</email:CompleteTime\>

     \<tasks:StartDate\>2009-02-19T08:00:00.000Z\</tasks:StartDate\>

     \<tasks:UtcStartDate\>2009-02-19T08:00:00.000Z\</tasks:UtcStartDate\>

     \<tasks:DueDate\>2009-02-19T08:00:00.000Z\</tasks:DueDate\>

     \<tasks:UtcDueDate\>2009-02-19T08:00:00.000Z\</tasks:UtcDueDate\>

     \<tasks:DateCompleted\>2009-02-19T09:30:00.000Z\</tasks:DateCompleted\>

     \<tasks:ReminderSet\>0\</tasks:ReminderSet\>

     \<tasks:ReminderTime\>2009-02-24T20:00:00.000Z\</tasks:ReminderTime\>

     \<tasks:Subject\>Please follow up\</tasks:Subject\>

     \</email:Flag\>

     \</ApplicationData\>

     \</Change\>

     \</Commands\>

     \</Collection\>

     \</Collections\>

     \</Sync\>

### Clearing a Flag

The following example shows a **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) sent from the client to the server to clear a flag on an e-mail item. The **Status** element (section [2.2.2.74](#Section_20fba2e8af71412abb1b29c8d0d3fd2d)) value of 0 (zero) indicates that the flag is cleared.

548. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<Sync xmlns:email=\"Email:\" xmlns:tasks=\"Tasks:\" xmlns=\"AirSync:\"\>

     \<Collections\>

     \<Collection\>

     \<SyncKey\>1401532757\</SyncKey\>

     \<CollectionId\>5\</CollectionId\>

     \<DeletesAsMoves\>1\</DeletesAsMoves\>

     \<GetChanges\>1\</GetChanges\>

     \<WindowSize\>512\</WindowSize\>

     \<Commands\>

     \<Change\>

     \<ServerId\>5:5\</ServerId\>

     \<ApplicationData\>

     \<email:Read\>1\</email:Read\>

     \<email:Flag\>

     \<email:Status\>0\</email:Status\>

     \<email:FlagType\>Flag for follow up\</email:FlagType\>

     \<email:CompleteTime\>2009-02-19T08:30:00.000Z\</email:CompleteTime\>

     \<tasks:StartDate\>2009-02-19T08:00:00.000Z\</tasks:StartDate\>

     \<tasks:UtcStartDate\>2009-02-19T08:00:00.000Z\</tasks:UtcStartDate\>

     \<tasks:DueDate\>2009-02-19T08:00:00.000Z\</tasks:DueDate\>

     \<tasks:UtcDueDate\>2009-02-19T08:00:00.000Z\</tasks:UtcDueDate\>

     \<tasks:DateCompleted\>2009-02-20T09:30:00.000Z\</tasks:DateCompleted\>

     \<tasks:ReminderSet\>0\</tasks:ReminderSet\>

     \<tasks:ReminderTime\>2009-02-24T20:00:00.000Z\</tasks:ReminderTime\>

     \<tasks:Subject\>Please follow up\</tasks:Subject\>

     \</email:Flag\>

     \</ApplicationData\>

     \</Change\>

     \</Commands\>

     \</Collection\>

     \</Collections\>

     \</Sync\>

## Converting a GlobalObjId to a UID

The following examples demonstrate how to convert a **GlobalObjId** value to a **UID** value. For more information about the process used for this conversion, see section [3.1.4.7](#Section_dbbd92730cc248329f5f39caf3a1067d).

**Example 1**

1.  Given the following **GlobalObjId** value:

```{=html}
<!-- -->
```
581. GlobalObjID=BAAAAIIA4AB0xbcQGoLgCAfUCRDgQMnBJoXEAQAAAAAAAAAAEAAAAAvw7UtuTulOnjnjhns3jvM=

```{=html}
<!-- -->
```
2.  Decoded from [**base64 encoding**](#gt_179b9392-9019-45a3-880b-26f6890522b7), the **GlobalObjID** is equal to:

```{=html}
<!-- -->
```
582. Bytes 1-16: \<04\>\<00\>\<00\>\<00\>\<82\>\<00\>\<E0\>\<00\>\<74\>\<C5\>\<B7\>\<10\>\<1A\>\<82\>\<E0\>\<08\>

     Bytes 17-20: \<07\>\<D4\>\<09\>\<10\>

     Bytes 21-36: \<E0\>\<40\>\<C9\>\<C1\>\<26\>\<85\>\<C4\>\<01\>\<00\>\<00\>\<00\>\<00\>\<00\>\<00\>\<00\>\<00\>

     Bytes 37-40: \<10\>\<00\>\<00\>\<00\>

     Bytes 41-56: \<0B\>\<F0\>\<ED\>\<4B\>\<6E\>\<4E\>\<E9\>\<4E\>\<9E\>\<39\>\<E3\>\<86\>\<7B\>\<37\>\<8E\>\<F3\>

```{=html}
<!-- -->
```
3.  Because bytes 41-48 do not equal \"vCal-Uid\", this **GlobalObjId** is an OutlookID. Bytes 17-20 are converted to zeros and the entire value is hex encoded, resulting in the following **UID** value:

```{=html}
<!-- -->
```
587. UID=040000008200E00074C5B7101A82E00800000000E040C9C12685C4010000000000000000100000000BF0ED4B6E4EE94E9E39E3867B378EF3

**Example 2**

1.  Given the following **GlobalObjId** value:

```{=html}
<!-- -->
```
588. GlobalObjID=BAAAAIIA4AB0xbcQGoLgCAAAAAAAAAAAAAAAAAAAAAAAAAAAMwAAAHZDYWwtVWlkAQAAAHs4MTQxMkQzQy0yQTI0LTRFOUQtQjIwRS0xMUY3QkJFOTI3OTl9AA==

```{=html}
<!-- -->
```
2.  Decoded from base64 encoding, the **GlobalObjID** is equal to:

```{=html}
<!-- -->
```
589. Bytes 1-16: \<04\>\<00\>\<00\>\<00\>\<82\>\<00\>\<E0\>\<00\>\<74\>\<C5\>\<B7\>\<10\>\<1A\>\<82\>\<E0\>\<08\>

     Bytes 17-20: \<00\>\<00\>\<00\>\<00\>

     Bytes 21-36: \<00\>\<00\>\<00\>\<00\>\<00\>\<00\>\<00\>\<00\>\<00\>\<00\>\<00\>\<00\>\<00\>\<00\>\<00\>\<00\>

     Bytes 37-40: \<33\>\<00\>\<00\>\<00\>

     Bytes 41-52: vCal-Uid\<01\>\<00\>\<00\>\<00\>

     Bytes 53-91: {81412D3C-2A24-4E9D-B20E-11F7BBE92799}\<00\>

```{=html}
<!-- -->
```
3.  Bytes 37-40 indicate the length of the data to follow is 51 bytes. 51 -- 13 = 38 bytes for the length of the **UID**.

4.  The 38 bytes beginning at byte 53 result in the **UID** value:

```{=html}
<!-- -->
```
595. UID={81412D3C-2A24-4E9D-B20E-11F7BBE92799}

## Adding a Draft Email with Attachments

The following example demonstrates a client request to synchronize a draft email to the [**Drafts folder**](#gt_97c27c06-f5e7-4eae-a54e-1839d41f69dc) on the server, and the server response. The draft email has three attachments.

The **Sync** command request ([\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 2.2.1.21) includes and an **airsyncbase:Add** element ([\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 2.2.2.2) for each attachment and the **airsync:CollectionId** element (\[MS-ASCMD\] section 2.2.3.30.6) with the value 4 to specify the Drafts folder.

The **Sync** command response includes the **ConversationId** (section [2.2.2.21](#Section_588e67a134184965aa3dfb3e7d5efdab)) and the **ConversationIndex** (section [2.2.2.22](#Section_ee3ec0eb27504da6ab66819644b6ffba)) elements for the draft email, and a **airsyncbase:FileReference** element (\[MS-ASAIRS\] section 2.2.2.24.1) for each attachment.

Request:

596. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<Sync xmlns:email=\"Email:\" xmlns:airsyncbase=\"AirSyncBase:\" xmlns:email2=\"Email2:\" xmlns=\"AirSync:\" \>

     \<Collections\>

     \<Collection\>

     \<SyncKey\>1751740540\</SyncKey\>

     \<CollectionId\>4\</CollectionId\>

     \<GetChanges\>1\</GetChanges\>

     \<Commands\>

     \<Add\>

     \<ClientId\>1\</ClientId\>

     \<ApplicationData\>

     \<email:To\>\"Device User\" &lt;someone1@example.com&gt;\</email:To\>

     \<email:CC\>\"Device User 2\" &lt;someone2@example.com&gt;\</email:CC\>

     \<email2:Bcc\>\"Device User 3\" &lt;someone3@example.com&gt;\</email2:Bcc\>

     \<email:ReplyTo\>\"Device User\" &lt;someone1@example.com&gt;\</email:ReplyTo\>

     \<email:Subject\>New draft message\</email:Subject\>

     \<airsyncbase:Body\>

     \<airsyncbase:Type\>2\</airsyncbase:Type\>

     \<airsyncbase:Data\>

     &lt;html&gt;

     &lt;head&gt;

     &lt;meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"&gt;

     &lt;style type=\"text/css\" style=\"\"&gt;

     &lt;!\--

     p

     {margin-top:0px;

     margin-bottom:0px}

     \--&gt;

     &lt;/style&gt;

     &lt;/head&gt;

     &lt;body dir=\"ltr\"&gt;

     &lt;div id=\"OWAFontStyleDivID\" style=\"font-size:12pt; color:#000000; background-color:#FFFFFF; font-family:Calibri,Arial,Helvetica,sans-serif\"&gt;

     &lt;p&gt;&amp;nbsp;&lt;/p&gt;

     &lt;p&gt;Test draft email&lt;/p&gt;

     &lt;p&gt;&amp;nbsp;&lt;/p&gt;

     &lt;p&gt;&lt;img title=\"Settings.png\" name=\"null\" src=\"cid:febc806d-5c32-43a1-9d07-759471ea18cd\"&gt;&lt;/p&gt;

     &lt;p&gt;&lt;/p&gt;

     &lt;/div&gt;

     &lt;/body&gt;

     &lt;/html&gt;

     \</airsyncbase:Data\>

     \</airsyncbase:Body\>

     \<email:Importance\>1\</email:Importance\>

     \<email:Read\>0\</email:Read\>

     \<airsyncbase:Attachments\>

     \<airsyncbase:Add\>

     \<airsyncbase:ClientId\>0a450577-71d6-41b3-ac12-98717a3a95d7\</airsyncbase:ClientId\>

     \<airsyncbase:Method\>1\</airsyncbase:Method\>

     \<airsyncbase:ContentType\>text/plain\</airsyncbase:ContentType\>

     \<airsyncbase:Content\> exampleTextContents \</airsyncbase:Content\>

     \<airsyncbase:DisplayName\>test100.txt\</airsyncbase:DisplayName\>

     \</airsyncbase:Add\>

     \<airsyncbase:Add\>

     \<airsyncbase:ClientId\>4c5bcc06-0418-4ce8-a364-408925025ce1\</airsyncbase:ClientId\>

     \<airsyncbase:Method\>1\</airsyncbase:Method\>

     \<airsyncbase:ContentType\>image/jpeg\</airsyncbase:ContentType\>

     \<airsyncbase:ContentId\>febc806d-5c32-43a1-9d07-759471ea18cd\</airsyncbase:ContentId\>

     \<airsyncbase:Content\>% PNG \[png content removed\] \</airsyncbase:Content\>

     \<airsyncbase:DisplayName\>image410.jpg\</airsyncbase:DisplayName\>

     \<airsyncbase:IsInline/\>

     \</airsyncbase:Add\>

     \<airsyncbase:Add\>

     \<airsyncbase:ClientId\>92e9835a-4345-4fe6-a287-337a21d01640\</airsyncbase:ClientId\>

     \<airsyncbase:Method\>5\</airsyncbase:Method\>

     \<airsyncbase:Content\>

     Subject: Test email

     Thread-Topic: Test email

     Thread-Index: Ac9D1vBAPd2rYcUxRoaP0UbT6rBUcg==

     Date: Wed, 19 Mar 2014 17:54:29 -0700

     \[remaining MIME email content removed\]

     \</airsyncbase:Content\>

     \<airsyncbase:DisplayName\>EmailAttachment450.eml\</airsyncbase:DisplayName\>

     \</airsyncbase:Add\>

     \</airsyncbase:Attachments\>

     \<email:Flag/\>

     \</ApplicationData\>

     \</Add\>

     \</Commands\>

     \</Collection\>

     \</Collections\>

     \</Sync\>

Response:

676. \<?xml version=\"1.0\" encoding=\"utf-8\"?\>

     \<Sync xmlns:airsyncbase=\"AirSyncBase:\" xmlns:email2=\"Email2:\" xmlns=\"AirSync:\"\>

     \<Collections\>

     \<Collection\>

     \<SyncKey\>1646260323\</SyncKey\>

     \<CollectionId\>4\</CollectionId\>

     \<Status\>1\</Status\>

     \<Responses\>

     \<Add\>

     \<ClientId\>1\</ClientId\>

     \<ServerId\>4:1\</ServerId\>

     \<Status\>1\</Status\>

     \<ApplicationData\>

     \<email2:ConversationId\>‚&#x1F;&#x7;\]¢ÎÎI¶□wfê3,À\</email2:ConversationId\>

     \<email2:ConversationIndex\>&#x1;&#x1;Ð¢Dt‚&#x1F;&#x7;\]¢ÎÎI¶□wfê3,À\</email2:ConversationIndex\>

     \<airsyncbase:Attachments\>

     \<airsyncbase:Attachment\>

     \<airsyncbase:ClientId\>0a450577-71d6-41b3-ac12-98717a3a95d7\</airsyncbase:ClientId\>

     \<airsyncbase:FileReference\>16%3aX%3aRgAAAABfKyfUx4%2ffSbR2FcPpk3NzBwCEwQOi7xYERpHe4Z0%2fO1GWAAAAAAEPAACEwQOi7xYERpHe4Z0%2fO1GWAAAAAGMaAAAJ%3aEABMHNZXfsMSSZMxtYg%2bylB2\</airsyncbase:FileReference\>

     \</airsyncbase:Attachment\>

     \<airsyncbase:Attachment\>

     \<airsyncbase:ClientId\>4c5bcc06-0418-4ce8-a364-408925025ce1\</airsyncbase:ClientId\>

     \<airsyncbase:FileReference\>16%3aX%3aRgAAAABfKyfUx4%2ffSbR2FcPpk3NzBwCEwQOi7xYERpHe4Z0%2fO1GWAAAAAAEPAACEwQOi7xYERpHe4Z0%2fO1GWAAAAAGMaAAAJ%3aEADnMXCIdYwQRY64DiAVryq%2f\</airsyncbase:FileReference\>

     \</airsyncbase:Attachment\>

     \<airsyncbase:Attachment\>

     \<airsyncbase:ClientId\>92e9835a-4345-4fe6-a287-337a21d01640\</airsyncbase:ClientId\>

     \<airsyncbase:FileReference\>16%3aX%3aRgAAAABfKyfUx4%2ffSbR2FcPpk3NzBwCEwQOi7xYERpHe4Z0%2fO1GWAAAAAAEPAACEwQOi7xYERpHe4Z0%2fO1GWAAAAAGMaAAAJ%3aEAB3OH9leoMyQ5fU9JEQ9kv9\</airsyncbase:FileReference\>

     \</airsyncbase:Attachment\>

     \</airsyncbase:Attachments\>

     \</ApplicationData\>

     \</Add\>

     \</Responses\>

     \</Collection\>

     \</Collections\>

     \</Sync\>

# Security

## Security Considerations for Implementers

None.

## Index of Security Parameters

None.

# Appendix A: Full XML Schema

For ease of implementation, the following sections provide the full [**XML schemas**](#gt_bd0ce6f9-c350-4900-827e-951265294067) for this protocol. Unless otherwise specified, these schemas are valid only for protocol versions 2.5, 12.0, 12.1, 14.0, 14.1, 16.0, and 16.1.

  ----------------------------------------------------------------------------------------------------
  Schema name               Prefix                  Section
  ------------------------- ----------------------- --------------------------------------------------
  Email namespace schema    email                   [6.1](#Section_efab33ae5c124143b554a1b09088e48d)

  Email2 namespace schema   email2                  [6.2](#Section_32114c14180141a58afeea7f18e4d6ba)
  ----------------------------------------------------------------------------------------------------

## Email Namespace Schema

This section contains the contents of the Email.xsd. The additional files that this schema file requires to operate correctly are listed in the following table.

  -----------------------------------------------------------------------------------------------------------------------------
  File name                           Defining section/specification
  ----------------------------------- -----------------------------------------------------------------------------------------
  AirSyncBase.xsd                     [\[MS-ASAIRS\]](%5bMS-ASAIRS%5d.pdf#Section_d1ba798741bf483d904596dfe11e3d1c) section 6

  Calendar.xsd                        [\[MS-ASCAL\]](%5bMS-ASCAL%5d.pdf#Section_0c4486824a6a459aae662fed0712bef9) section 6

  Email2.xsd                          [6.2](#Section_32114c14180141a58afeea7f18e4d6ba)

  Tasks.xsd                           [\[MS-ASTASK\]](%5bMS-ASTASK%5d.pdf#Section_b8fe266450ba4d00bf6be4deab352c89) section 6

  ComposeMail.xsd                     [\[MS-ASCMD\]](%5bMS-ASCMD%5d.pdf#Section_1a3490f1afe1418aaa926f630036d65a) section 6.5

  MeetingResponseRequest.xsd          \[MS-ASCMD\] section 6.25
  -----------------------------------------------------------------------------------------------------------------------------

710. \<?xml version=\"1.0\" encoding=\"UTF-8\"?\>

     \<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:airsyncbase=

     \"AirSyncBase\" xmlns:calendar=\"Calendar\" xmlns:email2=\"Email2\"

     xmlns:tasks=\"Tasks\" xmlns=\"Email\" targetNamespace=\"Email\"

     elementFormDefault=\"qualified\" attributeFormDefault=\"unqualified\"\>

     \<xs:import namespace=\"AirSyncBase\" schemaLocation=\"AirSyncBase.xsd\"/\>

     \<xs:import namespace=\"Calendar\" schemaLocation=\"Calendar.xsd\"/\>

     \<xs:import namespace=\"Email2\" schemaLocation=\"Email2.xsd\"/\>

     \<xs:import namespace=\"Tasks\" schemaLocation=\"Tasks.xsd\"/\>

     \<xs:import namespace=\"ComposeMail\" schemaLocation=\"ComposeMail.xsd\"/\>

     \<xs:import namespace=\"MeetingResponse\" schemaLocation=\"MeetingResponseRequest.xsd\"/\>

     \<xs:element name=\"To\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:string\"\>

     \<xs:maxLength value=\"32768\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element name=\"Cc\" type=\"xs:string\"/\>

     \<xs:element name=\"From\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:string\"\>

     \<xs:maxLength value=\"32768\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element name=\"Subject\" type=\"xs:string\"/\>

     \<xs:element name=\"ReplyTo\" type=\"xs:string\"/\>

     \<xs:element name=\"DateReceived\" type=\"xs:dateTime\"/\>

     \<xs:element name=\"DisplayTo\" type=\"xs:string\"/\>

     \<xs:element name=\"ThreadTopic\" type=\"xs:string\"/\>

     \<xs:element name=\"Importance\" type=\"xs:unsignedByte\"/\>

     \<xs:element name=\"Read\" type=\"xs:boolean\"/\>

     \<xs:element name=\"MessageClass\" type=\"xs:string\"/\>

     \<xs:element name=\"MeetingRequest\"\>

     \<xs:complexType\>

     \<xs:sequence\>

     \<xs:element name=\"AllDayEvent\" type=\"xs:unsignedByte\" minOccurs=\"0\"/\>

     \<xs:element name=\"StartTime\" type=\"xs:dateTime\" minOccurs=\"0\"/\>

     \<xs:element name=\"DtStamp\" type=\"xs:dateTime\"/\>

     \<xs:element name=\"EndTime\" type=\"xs:dateTime\" minOccurs=\"0\"/\>

     \<xs:element name=\"InstanceType\" type=\"xs:unsignedByte\" minOccurs=\"0\"/\>

     \<xs:element name=\"Location\" minOccurs=\"0\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:string\"\>

     \<xs:maxLength value=\"32768\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element ref=\"airsyncbase:Location\" minOccurs=\"0\"/\>

     \<xs:element name=\"Organizer\" type=\"xs:string\" minOccurs=\"0\"/\>

     \<xs:element name=\"RecurrenceId\" type=\"xs:dateTime\" minOccurs=\"0\"/\>

     \<xs:element name=\"Reminder\" type=\"xs:unsignedShort\" minOccurs=\"0\"/\>

     \<xs:element name=\"ResponseRequested\" type=\"xs:unsignedByte\"

     minOccurs=\"0\"/\>

     \<xs:element name=\"Recurrences\" minOccurs=\"0\"\>

     \<xs:complexType\>

     \<xs:sequence\>

     \<xs:element name=\"Recurrence\"\>

     \<xs:complexType\>

     \<xs:sequence\>

     \<xs:element name=\"Type\" type=\"xs:unsignedByte\"/\>

     \<xs:element name=\"Interval\" type=\"xs:integer\"/\>

     \<xs:element name=\"Until\" type=\"xs:string\" minOccurs=\"0\"/\>

     \<xs:element name=\"Occurrences\" type=\"xs:integer\"

     minOccurs=\"0\"/\>

     \<xs:element name=\"WeekOfMonth\" type=\"xs:integer\"

     minOccurs=\"0\"/\>

     \<xs:element name=\"DayOfMonth\" type=\"xs:integer\"

     minOccurs=\"0\"/\>

     \<xs:element name=\"DayOfWeek\" type=\"xs:integer\"

     minOccurs=\"0\"/\>

     \<xs:element name=\"MonthOfYear\" type=\"xs:integer\"

     minOccurs=\"0\"/\>

     \<xs:element ref=\"email2:CalendarType\" minOccurs=\"0\"/\>

     \<xs:element ref=\"email2:IsLeapMonth\" minOccurs=\"0\"/\>

     \<xs:element ref=\"email2:FirstDayOfWeek\" minOccurs=\"0\"/\>

     \</xs:sequence\>

     \</xs:complexType\>

     \</xs:element\>

     \</xs:sequence\>

     \</xs:complexType\>

     \</xs:element\>

     \<xs:element name=\"Sensitivity\" type=\"xs:integer\" minOccurs=\"0\"/\>

     \<xs:element name=\"BusyStatus\" type=\"xs:integer\" minOccurs=\"0\"/\>

     \<xs:element name=\"TimeZone\" type=\"xs:string\"/\>

     \<xs:element name=\"GlobalObjId\" type=\"xs:string\" minOccurs=\"0\"/\>

     \<xs:element name=\"DisallowNewTimeProposal\" type=\"xs:unsignedByte\"

     minOccurs=\"0\"/\>

     \<xs:element ref=\"email2:MeetingMessageType\" minOccurs=\"1\"/\>

     \<xs:element ref=\"calendar:UID\" minOccurs=\"0\"/\>

     \<xs:element ref=\"MeetingResponse:ProposedStartTime\" minOccurs=\"0\"/\>

     \<xs:element ref=\"MeetingResponse:ProposedEndTime\" minOccurs=\"0\"/\>

     \<xs:element ref=\"ComposeMail:Forwardees\" minOccurs=\"0\"/\>

     \</xs:sequence\>

     \</xs:complexType\>

     \</xs:element\>

     \<xs:element name=\"InternetCPID\" type=\"xs:string\"/\>

     \<xs:element name=\"Flag\"\>

     \<xs:complexType\>

     \<xs:sequence\>

     \<xs:element ref=\"tasks:Subject\" minOccurs=\"0\"/\>

     \<xs:element name=\"Status\" type=\"xs:integer\" minOccurs=\"0\"/\>

     \<xs:element name=\"FlagType\" type=\"xs:string\" minOccurs=\"0\"/\>

     \<xs:element ref=\"tasks:DateCompleted\" minOccurs=\"0\"/\>

     \<xs:element name=\"CompleteTime\" type=\"xs:dateTime\" minOccurs=\"0\"/\>

     \<xs:element ref=\"tasks:StartDate\" minOccurs=\"0\"/\>

     \<xs:element ref=\"tasks:DueDate\" minOccurs=\"0\"/\>

     \<xs:element ref=\"tasks:UtcStartDate\" minOccurs=\"0\"/\>

     \<xs:element ref=\"tasks:UtcDueDate\" minOccurs=\"0\"/\>

     \<xs:element ref=\"tasks:ReminderSet\" minOccurs=\"0\"/\>

     \<xs:element ref=\"tasks:ReminderTime\" minOccurs=\"0\"/\>

     \<xs:element ref=\"tasks:OrdinalDate\" minOccurs=\"0\"/\>

     \<xs:element ref=\"tasks:SubOrdinalDate\" minOccurs=\"0\"/\>

     \</xs:sequence\>

     \</xs:complexType\>

     \</xs:element\>

     \<xs:element name=\"ContentClass\" type=\"xs:string\"/\>

     \<xs:element name=\"Categories\"\>

     \<xs:complexType\>

     \<xs:sequence\>

     \<xs:element name=\"Category\" type=\"xs:string\" minOccurs=\"0\"

     maxOccurs=\"300\"/\>

     \</xs:sequence\>

     \</xs:complexType\>

     \</xs:element\>

     \<xs:element name=\"Attachments\"\>

     \<xs:complexType\>

     \<xs:sequence\>

     \<xs:element name=\"Attachment\" minOccurs=\"1\" maxOccurs=\"unbounded\"\>

     \<xs:complexType\>

     \<xs:sequence\>

     \<xs:element name=\"AttName\" type=\"xs:string\"/\>

     \<xs:element name=\"AttSize\" type=\"xs:integer\"/\>

     \<xs:element name=\"AttMethod\" type=\"xs:unsignedByte\"/\>

     \<xs:element name=\"DisplayName\" type=\"xs:string\" minOccurs=\"0\"/\>

     \</xs:sequence\>

     \</xs:complexType\>

     \</xs:element\>

     \</xs:sequence\>

     \</xs:complexType\>

     \</xs:element\>

     \<xs:element name=\"Body\" type=\"xs:string\"/\>

     \<xs:element name=\"BodySize\" type=\"xs:integer\"/\>

     \<xs:element name=\"BodyTruncated\" type=\"xs:boolean\"/\>

     \<xs:element name=\"MIMEData\" type=\"xs:string\"/\>

     \<xs:element name=\"MIMESize\" type=\"xs:integer\"/\>

     \<xs:element name=\"MIMETruncated\" type=\"xs:boolean\"/\>

     \<xs:group name=\"AllProps\"\>

     \<xs:sequence\>

     \<xs:choice maxOccurs=\"unbounded\"\>

     \<xs:element ref=\"To\"/\>

     \<xs:element ref=\"Cc\"/\>

     \<xs:element ref=\"From\"/\>

     \<xs:element ref=\"Subject\"/\>

     \<xs:element ref=\"ReplyTo\"/\>

     \<xs:element ref=\"DateReceived\"/\>

     \<xs:element ref=\"DisplayTo\"/\>

     \<xs:element ref=\"ThreadTopic\"/\>

     \<xs:element ref=\"Importance\"/\>

     \<xs:element ref=\"Read\"/\>

     \<xs:element ref=\"MessageClass\"/\>

     \<xs:element ref=\"MeetingRequest\"/\>

     \<xs:element ref=\"InternetCPID\"/\>

     \<xs:element ref=\"Flag\"/\>

     \<xs:element ref=\"ContentClass\"/\>

     \<xs:element ref=\"Categories\"/\>

     \<xs:element ref=\"Attachments\"/\>

     \<xs:element ref=\"Body\"/\>

     \<xs:element ref=\"BodySize\"/\>

     \<xs:element ref=\"BodyTruncated\"/\>

     \<xs:element ref=\"MIMEData\"/\>

     \<xs:element ref=\"MIMESize\"/\>

     \<xs:element ref=\"MIMETruncated\"/\>

     \</xs:choice\>

     \</xs:sequence\>

     \</xs:group\>

     \<xs:group name=\"TopLevelSchemaProps\"\>

     \<xs:sequence\>

     \<xs:choice maxOccurs=\"unbounded\"\>

     \<xs:element name=\"To\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Cc\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"From\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"ReplyTo\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"DateReceived\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Subject\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"DisplayTo\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Importance\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"Read\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"MessageClass\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"MeetingRequest\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"ThreadTopic\" type=\"airsyncbase:EmptyTag\"/\>

     \<xs:element name=\"InternetCPID\" type=\"airsyncbase:EmptyTag\"/\>

     \</xs:choice\>

     \</xs:sequence\>

     \</xs:group\>

     \<xs:group name=\"ComparisonProps\"\>

     \<xs:sequence\>

     \<xs:choice maxOccurs=\"unbounded\"\>

     \<xs:element name=\"DateReceived\" type=\"airsyncbase:EmptyTag\"/\>

     \</xs:choice\>

     \</xs:sequence\>

     \</xs:group\>

     \</xs:schema\>

## Email2 Namespace Schema

This section contains the contents of the Email2.xsd file.

914. \<?xml version=\"1.0\" encoding=\"UTF-8\"?\>

     \<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"Email2\"

     targetNamespace=\"Email2\" elementFormDefault=\"qualified\"

     attributeFormDefault=\"unqualified\"\>

     \<xs:element name=\"UmCallerID\" type=\"xs:string\"/\>

     \<xs:element name=\"UmUserNotes\" type=\"xs:string\"/\>

     \<xs:element name=\"UmAttDuration\" type=\"xs:integer\"/\>

     \<xs:element name=\"UmAttOrder\" type=\"xs:integer\"/\>

     \<xs:element name=\"ConversationId\" type=\"xs:string\"/\>

     \<xs:element name=\"ConversationIndex\" type=\"xs:string\"/\>

     \<xs:element name=\"LastVerbExecuted\" type=\"xs:integer\"/\>

     \<xs:element name=\"LastVerbExecutionTime\" type=\"xs:dateTime\"/\>

     \<xs:element name=\"ReceivedAsBcc\" type=\"xs:boolean\"/\>

     \<xs:element name=\"Sender\" type=\"xs:string\"/\>

     \<xs:element name=\"CalendarType\" type=\"xs:integer\"/\>

     \<xs:element name=\"IsLeapMonth\" type=\"xs:unsignedByte\"/\>

     \<xs:element name=\"AccountId\" type=\"xs:string\"/\>

     \<xs:element name=\"FirstDayOfWeek\" type=\"xs:unsignedByte\"/\>

     \<xs:element name=\"MeetingMessageType\" type=\"xs:unsignedByte\"/\>

     \<xs:element name=\"Bcc\"\>

     \<xs:simpleType\>

     \<xs:restriction base=\"xs:string\"\>

     \<xs:maxLength value=\"1024\"/\>

     \</xs:restriction\>

     \</xs:simpleType\>

     \</xs:element\>

     \<xs:element name=\"IsDraft\" type=\"xs:boolean\"\>

     \<xs:element name=\"Send\"/\>

     \<xs:group name=\"AllProps\"\>

     \<xs:sequence\>

     \<xs:choice maxOccurs=\"unbounded\"\>

     \<xs:element ref=\"UmCallerID\"/\>

     \<xs:element ref=\"UmUserNotes\"/\>

     \<xs:element ref=\"UmAttDuration\"/\>

     \<xs:element ref=\"UmAttOrder\"/\>

     \<xs:element ref=\"ConversationId\"/\>

     \<xs:element ref=\"ConversationIndex\"/\>

     \<xs:element ref=\"LastVerbExecuted\"/\>

     \<xs:element ref=\"LastVerbExecutionTime\"/\>

     \<xs:element ref=\"ReceivedAsBcc\"/\>

     \<xs:element ref=\"Sender\"/\>

     \<xs:element ref=\"CalendarType\"/\>

     \<xs:element ref=\"IsLeapMonth\"/\>

     \<xs:element ref=\"AccountId\"/\>

     \<xs:element ref=\"FirstDayOfWeek\"/\>

     \<xs:element ref=\"MeetingMessageType\"/\>

     \<xs:element ref=\"Bcc\"/\>

     \<xs:element ref=\"IsDraft\"/\>

     \<xs:element ref=\"Send\"/\>

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

[\<1\> Section 2.2.2.47](\l): This value 6 is supported only in Exchange 2010.

[\<2\> Section 3.1.4.7](\l): An OutlookID is an identifier set by Microsoft Office Outlook 2003, Microsoft Office Outlook 2007, Microsoft Outlook 2010, Microsoft Outlook 2013, Microsoft Outlook 2016, or Microsoft Outlook 2019.

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
  [7](#Section_75ada305e444410787b9dd2dcb31d75f) Appendix B: Product Behavior   Updated list of supported products.   Major

  ------------------------------------------------------------------------------------------------------------------------------------

# Index

A

Abstract data model

[client](#abstract-data-model) 84

[server](#abstract-data-model-1) 89

[Adding a draft email with attachments example](#adding-a-draft-email-with-attachments) 108

[Applicability](#applicability-statement) 12

C

[Capability negotiation](#versioning-and-capability-negotiation) 12

[Change tracking](#change-tracking) 118

[Clearing a flag example](#clearing-a-flag) 107

Client

[abstract data model](#abstract-data-model) 84

[initialization](#initialization) 84

[message processing](#message-processing-events-and-sequencing-rules) 86

[other local events](#other-local-events) 89

[sequencing rules](#message-processing-events-and-sequencing-rules) 86

[timer events](#timer-events) 89

[timers](#timers) 84

[Converting a GlobalObjId to a UID example](#converting-a-globalobjid-to-a-uid) 107

D

Data model - abstract

[client](#abstract-data-model) 84

[server](#abstract-data-model-1) 89

[Deleting an e-mail example](#deleting-an-e-mail) 100

E

Elements

[AccountId](#accountid) 17

[AllDayEvent](#alldayevent) 18

[Attachment](#attachment) 19

[Attachments](#attachments-airsyncbase-namespace) 19

[Body](#body-airsyncbase-namespace) 24

[BodyPart](#bodypart) 26

[BusyStatus](#busystatus) 28

[CalendarType](#calendartype) 28

[Categories](#categories) 30

[Category](#category) 30

[Cc](#cc) 31

[CompleteTime](#completetime) 31

[ContentClass](#contentclass) 32

[ConversationId](#conversationid) 33

[ConversationIndex](#conversationindex) 33

[DateCompleted](#datecompleted) 34

[DateReceived](#datereceived) 35

[DayOfMonth](#dayofmonth) 35

[DayOfWeek](#dayofweek) 36

[DisallowNewTimeProposal](#disallownewtimeproposal) 37

[DisplayName](#displayname) 37

[DisplayTo](#displayto) 38

[DtStamp](#dtstamp) 39

[DueDate](#duedate) 39

[EndTime](#endtime) 40

[FirstDayOfWeek](#firstdayofweek) 40

[Flag](#flag) 41

[FlagType](#flagtype) 42

[From](#from) 43

[GlobalObjId](#globalobjid) 44

[Importance](#importance) 45

[InstanceType](#instancetype) 46

[InternetCPID](#internetcpid) 47

[Interval](#interval) 47

[IsLeapMonth](#isleapmonth) 48

[LastVerbExecuted](#lastverbexecuted) 49

[LastVerbExecutionTime](#lastverbexecutiontime) 50

[Location](#location) 50

[MeetingMessageType](#meetingmessagetype) 51

[MeetingRequest](#meetingrequest) 52

[MessageClass](#messageclass) 53

[MonthOfYear](#monthofyear) 57

[NativeBodyType](#nativebodytype) 58

[Occurrences](#occurrences) 58

[OrdinalDate](#ordinaldate) 59

[Organizer](#organizer) 60

[Read](#read) 60

[ReceivedAsBcc](#receivedasbcc) 61

[Recurrence](#recurrence) 61

[RecurrenceId](#recurrenceid) 62

[Recurrences](#recurrences) 63

[Reminder](#reminder) 64

[ReminderSet](#reminderset) 64

[ReminderTime](#remindertime) 65

[ReplyTo](#replyto) 65

[ResponseRequested](#responserequested) 66

[RightsManagementLicense](#rightsmanagementlicense) 67

[Sender](#sender) 68

[Sensitivity](#sensitivity) 69

[StartDate](#startdate) 69

[StartTime](#starttime) 70

[Status](#status) 71

[Subject](#subject-email-namespace) 72

[SubOrdinalDate](#subordinaldate) 73

[ThreadTopic](#threadtopic) 73

[TimeZone](#timezone) 74

[To](#to) 75

[Type](#type) 75

[UmAttDuration](#umattduration) 76

[UmAttOrder](#umattorder) 77

[UmCallerID](#umcallerid) 77

[UmUserNotes](#umusernotes) 78

[Until](#until) 79

[UtcDueDate](#utcduedate) 80

[UtcStartDate](#utcstartdate) 81

[WeekOfMonth](#weekofmonth) 82

[Elements message](#elements) 13

Email Namespace Schema schema

[Full XML Schema:\\Email Namespace Schema schema](#email-namespace-schema) 112

Email2 Namespace Schema schema

[XML Schema:\\Email2 Namespace Schema schema](#email2-namespace-schema) 115

Examples

[adding a draft email with attachments](#adding-a-draft-email-with-attachments) 108

[clearing a flag](#clearing-a-flag) 107

[converting a GlobalObjId to a UID](#converting-a-globalobjid-to-a-uid) 107

[deleting an e-mail](#deleting-an-e-mail) 100

[marking a flag as complete](#marking-a-flag-as-complete) 106

[retrieving e-mail metadata and body](#retrieving-e-mail-metadata-and-body) 103

[setting a flag](#setting-a-flag) 105

[setting the flag on an e-mail](#setting-the-flag-on-an-e-mail) 105

[synchronizing e-mail attachments](#synchronizing-e-mail-attachments) 98

[synchronizing e-mail metadata and body](#synchronizing-e-mail-metadata-and-body) 97

[synchronizing meeting requests](#synchronizing-meeting-requests) 101

[synchronizing only e-mail metadata](#synchronizing-only-e-mail-metadata) 96

F

[Fields - vendor-extensible](#vendor-extensible-fields) 12

[Full XML schema](#appendix-a-full-xml-schema) 112

[XML schema](#appendix-a-full-xml-schema) 112

G

[Glossary](#glossary) 8

[Groups message](#groups) 82

I

[Implementer - security considerations](#security-considerations-for-implementers) 111

[Index of security parameters](#index-of-security-parameters) 111

[Informative references](#informative-references) 11

Initialization

[client](#initialization) 84

[server](#initialization-1) 89

[Introduction](#introduction) 8

M

[Marking a flag as complete example](#marking-a-flag-as-complete) 106

Message processing

[client](#message-processing-events-and-sequencing-rules) 86

[server](#message-processing-events-and-sequencing-rules-1) 90

Messages

[Elements](#elements) 13

[Groups](#groups) 82

[Namespaces](#namespaces) 13

[syntax](#message-syntax) 13

[transport](#transport) 13

N

[Namespaces message](#namespaces) 13

[Normative references](#normative-references) 10

O

Other local events

[client](#other-local-events) 89

[server](#other-local-events-1) 95

[Overview (synopsis)](#overview) 11

P

[Parameters - security index](#index-of-security-parameters) 111

[Preconditions](#prerequisitespreconditions) 12

[Prerequisites](#prerequisitespreconditions) 12

[Product behavior](#appendix-b-product-behavior) 117

R

[References](#references) 10

[informative](#informative-references) 11

[normative](#normative-references) 10

[Relationship to other protocols](#relationship-to-other-protocols) 11

[Retrieving e-mail metadata and body example](#retrieving-e-mail-metadata-and-body) 103

S

Security

[implementer considerations](#security-considerations-for-implementers) 111

[parameter index](#index-of-security-parameters) 111

Sequencing rules

[client](#message-processing-events-and-sequencing-rules) 86

[server](#message-processing-events-and-sequencing-rules-1) 90

Server

[abstract data model](#abstract-data-model-1) 89

[initialization](#initialization-1) 89

[message processing](#message-processing-events-and-sequencing-rules-1) 90

[other local events](#other-local-events-1) 95

[sequencing rules](#message-processing-events-and-sequencing-rules-1) 90

[timer events](#timer-events-1) 94

[timers](#timers-1) 89

[Setting a flag example](#setting-a-flag) 105

[Setting the flag on an e-mail examples](#setting-the-flag-on-an-e-mail) 105

[Standards assignments](#standards-assignments) 12

[Synchronizing e-mail attachments examples](#synchronizing-e-mail-attachments) 98

[Synchronizing e-mail metadata and body example](#synchronizing-e-mail-metadata-and-body) 97

[Synchronizing meeting requests examples](#synchronizing-meeting-requests) 101

[Synchronizing only e-mail metadata examples](#synchronizing-only-e-mail-metadata) 96

T

Timer events

[client](#timer-events) 89

[server](#timer-events-1) 94

Timers

[client](#timers) 84

[server](#timers-1) 89

[Tracking changes](#change-tracking) 118

[Transport](#transport) 13

V

[Vendor-extensible fields](#vendor-extensible-fields) 12

[Versioning](#versioning-and-capability-negotiation) 12

X

[XML schema](#appendix-a-full-xml-schema) 112
