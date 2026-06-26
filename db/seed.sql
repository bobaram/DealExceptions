-- Deal Exceptions Tracker — Seed Data
-- Normalised from legacy_deal_exceptions_export.csv and legacy_comments_export.csv.
--
-- Normalisation applied:
--   Priority : "H" -> High | "critical"/"Critical " -> Critical
--   Status   : "InReview" -> InReview | "APPROVED" -> Approved | "CLOSEd" -> Closed
--   Owner    : "Nomsa" / "nomsa.mokoena@sourcefin.example" -> "Nomsa Mokoena"
--   Dates    : three source formats unified to TIMESTAMPTZ
--   LegacyId : preserved for traceability back to the source system

-- Idempotent: only insert if table is empty
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM "DealExceptions" LIMIT 1) THEN
    RAISE NOTICE 'Seed data already present — skipping.';
    RETURN;
  END IF;

  INSERT INTO "DealExceptions"
    ("LegacyId","DealRef","ClientName","ExceptionType","Description","Priority","Status","AssignedOwner","CreatedAt","UpdatedAt","IsPossibleDuplicate")
  VALUES
    (1001,'SF-2026-00451','Mabena Transport CC',      'Affordability',    'Bank statements received but affordability calc missing latest month',                            'High',    'New',      'Nomsa Mokoena', '2026-06-14 09:15:00+00','2026-06-14 09:15:00+00', FALSE),
    (1002,'SF-2026-00452','Imbizo Hardware (Pty) Ltd', 'Credit Limit',     'Requested facility above normal credit limit; manual review required',                          'Critical','InReview', 'Thabo Dlamini', '2026-06-11 14:02:00+00','2026-06-19 16:31:00+00', FALSE),
    (1003,'SF-2026-00453','Langa Foods',               'Missing Docs',     'FICA docs not on file, but deal progressed to review',                                          'Medium',  'Approved', 'Busi Khumalo',  '2026-06-12 00:00:00+00','2026-06-18 00:00:00+00', FALSE),
    (1004,'SF-2026-00454','Blue Crane Logistics',      'Pricing Override', 'Rate override requested below floor',                                                            'High',    'Pending',  'Nomsa Mokoena', '2026-06-08 08:44:00+00','2026-06-13 11:10:00+00', FALSE),
    (1005,'SF-2026-00454','Blue Crane Logistics',      'Pricing Override', 'Duplicate row created from Excel import',                                                        'High',    'New',      'Nomsa Mokoena', '2026-06-08 08:45:00+00','2026-06-09 10:22:00+00', TRUE),
    (1006,'SF-2026-00455','Vela Medical Supplies',     'Director Approval','One director consent outstanding',                                                               'Low',     'Closed',   'Ayanda Ncube',  '2026-05-28 00:00:00+00','2026-06-02 00:00:00+00', FALSE),
    (1007,'SF-2026-00456','Northern Solar Installers', 'Affordability',    'Manual override requested because bank account includes once-off grant',                         'Critical','New',      NULL,            '2026-06-10 07:20:00+00','2026-06-12 13:02:00+00', FALSE),
    (1008,'SF-2026-00457','Kopano Retail Group',       'Data Mismatch',    'Client name differs between CRM and loan pack',                                                  'Medium',  'InReview', 'Thabo Dlamini', '2026-06-17 15:32:00+00','2026-06-19 09:09:00+00', FALSE),
    (1009,'SF-2026-00458','Umoya Farming',             'Security Docs',    'Surety signed but not witnessed',                                                                'Low',     'Rejected', 'Busi Khumalo',  '2026-06-18 10:00:00+00','2026-06-18 12:16:00+00', FALSE),
    (1010,'SF-2026-00459','Khanyisa Print Works',      'Affordability',    'Calculator in Excel gives different result from SharePoint column',                              'High',    'InReview', 'Ayanda Ncube',  '2026-06-13 16:40:00+00','2026-06-20 08:55:00+00', FALSE),
    (1011,'SF-2026-00460','Urban Renewals SA',         'Credit Limit',     'Manual exception created by email; missing from SharePoint until yesterday',                    'Critical','New',      'Justin Naidoo', '2026-06-05 11:30:00+00','2026-06-21 18:03:00+00', FALSE),
    (1012,'SF-2026-00461','Pula Office Supplies',      'Other',            'Business says ''just approve this one'' with no reason captured',                               'Medium',  'Closed',   'Nomsa Mokoena', '2026-06-01 09:00:00+00','2026-06-01 09:05:00+00', FALSE);

  INSERT INTO "Comments" ("LegacyId","ExceptionId","AuthorName","Text","CreatedAt")
  SELECT l."LegacyId", e."Id", l."AuthorName", l."Text", l."CreatedAt"
  FROM (VALUES
    (5001, 1001, 'Nomsa Mokoena', 'Created from PowerApps screen.',                                       '2026-06-14 09:15:00+00'::TIMESTAMPTZ),
    (5002, 1002, 'Justin Naidoo', 'Credit memo is in my mailbox; I will forward later.',                  '2026-06-12 10:21:00+00'),
    (5003, 1002, 'Thabo Dlamini', 'Still waiting for signed approval.',                                   '2026-06-19 16:31:00+00'),
    (5004, 1003, 'Busi Khumalo',  'Team lead approved conditional on documents.',                         '2026-06-18 08:12:00+00'),
    (5005, 1004, 'Finance User',  'Need margin impact before approval.',                                  '2026-06-13 11:10:00+00'),
    (5006, 1005, 'Nomsa Mokoena', 'Possible duplicate of SF-2026-00454 (Legacy ID 1004). Not sure which one is current.', '2026-06-09 10:22:00+00'),
    (5007, 1007, 'Ops User',      'No owner found; leaving blank for now.',                               '2026-06-12 13:02:00+00'),
    (5008, 1008, 'Thabo Dlamini', 'CRM trading name differs from application pack.',                      '2026-06-19 09:09:00+00'),
    (5009, 1010, 'Ayanda Ncube',  'Excel formula changed by business user last week.',                    '2026-06-20 08:55:00+00'),
    (5010, 1011, 'Justin Naidoo', 'Backfilled from email chain after the fact.',                          '2026-06-21 18:03:00+00')
  ) AS l("LegacyId", "ExceptionId", "AuthorName", "Text", "CreatedAt")
  JOIN "DealExceptions" e ON e."LegacyId" = l."ExceptionId";

  INSERT INTO "StatusHistories" ("ExceptionId","FromStatus","ToStatus","ChangedBy","ChangedAt","Notes")
  SELECT
    "Id",
    'New',
    "Status",
    'LegacyImport',
    "CreatedAt",
    CASE WHEN "Status" = 'New' THEN 'Created in legacy system'
         ELSE 'Imported from legacy PowerApps/SharePoint system'
    END
  FROM "DealExceptions";

END $$;
