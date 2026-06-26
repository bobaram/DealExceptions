-- Idempotent seed data — only inserts when table is empty
-- RunAlways/Secondary — NullJournal (runs every startup)
-- Source: normalised from legacy_deal_exceptions_export.csv
--   Priority : "H" -> High | "critical" -> Critical
--   Status   : "APPROVED" -> Approved | "CLOSEd" -> Closed
--   Owner    : variant spellings unified to display name
--   LegacyId : preserved for traceability back to PowerApps/SharePoint

IF NOT EXISTS (SELECT 1 FROM [dbo].[DealExceptions])
BEGIN
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();

    INSERT INTO [dbo].[DealExceptions]
        ([LegacyId], [DealRef], [ClientName], [ExceptionType], [Description], [Priority], [Status], [AssignedOwner], [CreatedAt], [UpdatedAt], [IsPossibleDuplicate])
    VALUES
        (1001, 'SF-2026-00451', 'Mabena Transport CC',       'Affordability',     'Bank statements received but affordability calc missing latest month.',                    'High',     'New',      'Nomsa Mokoena', @Now, @Now, 0),
        (1002, 'SF-2026-00452', 'Imbizo Hardware (Pty) Ltd', 'Credit Limit',      'Requested facility above normal credit limit; manual review required.',                   'Critical', 'InReview', 'Thabo Dlamini', @Now, @Now, 0),
        (1003, 'SF-2026-00453', 'Langa Foods',               'Missing Docs',      'FICA docs not on file, but deal progressed to review.',                                   'Medium',   'Approved', 'Busi Khumalo',  @Now, @Now, 0),
        (1004, 'SF-2026-00454', 'Blue Crane Logistics',      'Pricing Override',  'Rate override requested below floor.',                                                     'High',     'Pending',  'Nomsa Mokoena', @Now, @Now, 0),
        (1005, 'SF-2026-00454', 'Blue Crane Logistics',      'Pricing Override',  'Duplicate row created from Excel import.',                                                 'High',     'New',      'Nomsa Mokoena', @Now, @Now, 1),
        (1006, 'SF-2026-00455', 'Vela Medical Supplies',     'Director Approval', 'One director consent outstanding.',                                                        'Low',      'Closed',   'Ayanda Ncube',  @Now, @Now, 0),
        (1007, 'SF-2026-00456', 'Northern Solar Installers', 'Affordability',     'Manual override requested because bank account includes once-off grant.',                  'Critical', 'New',      NULL,            @Now, @Now, 0),
        (1008, 'SF-2026-00457', 'Kopano Retail Group',       'Data Mismatch',     'Client name differs between CRM and loan pack.',                                           'Medium',   'InReview', 'Thabo Dlamini', @Now, @Now, 0),
        (1009, 'SF-2026-00458', 'Umoya Farming',             'Security Docs',     'Surety signed but not witnessed.',                                                         'Low',      'Rejected', 'Busi Khumalo',  @Now, @Now, 0),
        (1010, 'SF-2026-00459', 'Khanyisa Print Works',      'Affordability',     'Calculator in Excel gives different result from SharePoint column.',                       'High',     'InReview', 'Ayanda Ncube',  @Now, @Now, 0),
        (1011, 'SF-2026-00460', 'Urban Renewals SA',         'Credit Limit',      'Manual exception created by email; missing from SharePoint until yesterday.',              'Critical', 'New',      'Justin Naidoo', @Now, @Now, 0),
        (1012, 'SF-2026-00461', 'Pula Office Supplies',      'Other',             'Business says ''just approve this one'' with no reason captured.',                         'Medium',   'Closed',   'Nomsa Mokoena', @Now, @Now, 0);

    INSERT INTO [dbo].[Comments] ([LegacyId], [ExceptionId], [AuthorName], [Text], [CreatedAt])
    SELECT l.[LegacyId], e.[Id], l.[AuthorName], l.[Text], @Now
    FROM (VALUES
        (5001, 1001, 'Nomsa Mokoena', 'Created from PowerApps screen.'),
        (5002, 1002, 'Justin Naidoo', 'Credit memo is in my mailbox; I will forward later.'),
        (5003, 1002, 'Thabo Dlamini', 'Still waiting for signed approval.'),
        (5004, 1003, 'Busi Khumalo',  'Team lead approved conditional on documents.'),
        (5005, 1004, 'Finance User',  'Need margin impact before approval.'),
        (5006, 1005, 'Nomsa Mokoena', 'Possible duplicate of SF-2026-00454 (Legacy ID 1004). Not sure which one is current.'),
        (5007, 1007, 'Ops User',      'No owner found; leaving blank for now.'),
        (5008, 1008, 'Thabo Dlamini', 'CRM trading name differs from application pack.'),
        (5009, 1010, 'Ayanda Ncube',  'Excel formula changed by business user last week.'),
        (5010, 1011, 'Justin Naidoo', 'Backfilled from email chain after the fact.')
    ) AS l ([LegacyId], [LegacyExceptionId], [AuthorName], [Text])
    JOIN [dbo].[DealExceptions] e ON e.[LegacyId] = l.[LegacyExceptionId];

    INSERT INTO [dbo].[StatusHistories] ([ExceptionId], [FromStatus], [ToStatus], [ChangedBy], [ChangedAt], [Notes])
    SELECT
        [Id],
        'New',
        [Status],
        'LegacyImport',
        @Now,
        CASE WHEN [Status] = 'New' THEN 'Created in legacy system'
             ELSE 'Imported from legacy PowerApps/SharePoint system'
        END
    FROM [dbo].[DealExceptions];
END
GO
