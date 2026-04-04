SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

SET IDENTITY_INSERT dbo.Clients ON;
GO

INSERT INTO dbo.Clients (ClientId, Name, Postcode, CreatedBy)
VALUES
    (3, 'Polaris Advisory', 'NW1 6XE', 'seed-data'),
    (4, 'Southbridge Retail', 'SR2 4PL', 'seed-data');
GO

SET IDENTITY_INSERT dbo.Clients OFF;
GO

INSERT INTO dbo.Organisations (ClientId, Name, Postcode, CreatedBy)
SELECT client_lookup.ClientId, organisation_seed.Name, organisation_seed.Postcode, 'seed-data'
FROM (VALUES
    ('Polaris Advisory', 'Unassigned mirrored forms', NULL),
    ('Polaris Advisory', 'Polaris Holdings', 'NW1 7AA'),
    ('Polaris Advisory', 'Polaris Services', 'NW1 8BB'),
    ('Southbridge Retail', 'Unassigned mirrored forms', NULL),
    ('Southbridge Retail', 'Southbridge Manufacturing', 'SR2 5CC'),
    ('Southbridge Retail', 'Southbridge Logistics', 'SR2 6DD')
) AS organisation_seed (ClientName, Name, Postcode)
INNER JOIN dbo.Clients AS client_lookup
    ON client_lookup.Name = organisation_seed.ClientName;
GO

INSERT INTO dbo.Contacts (ClientId, OrganisationId, FirstName, LastName, Email, Postcode, CreatedBy)
SELECT
    client_lookup.ClientId,
    organisation_lookup.OrganisationId,
    contact_seed.FirstName,
    contact_seed.LastName,
    contact_seed.Email,
    contact_seed.Postcode,
    'seed-data'
FROM (VALUES
    ('Polaris Advisory', 'Polaris Holdings', 'Alice', 'Bennett', 'alice.bennett@polaris.example', 'NW1 7AA'),
    ('Polaris Advisory', 'Polaris Holdings', 'Daniel', 'Cooper', 'daniel.cooper@polaris.example', 'NW1 7AA'),
    ('Polaris Advisory', 'Polaris Services', 'Priya', 'Shah', 'priya.shah@polaris.example', 'NW1 8BB'),
    ('Polaris Advisory', 'Polaris Services', 'Marcus', 'Reed', 'marcus.reed@polaris.example', 'NW1 8BB'),
    ('Southbridge Retail', 'Southbridge Manufacturing', 'Sophie', 'Turner', 'sophie.turner@southbridge.example', 'SR2 5CC'),
    ('Southbridge Retail', 'Southbridge Manufacturing', 'Leo', 'Watson', 'leo.watson@southbridge.example', 'SR2 5CC'),
    ('Southbridge Retail', 'Southbridge Logistics', 'Grace', 'Mitchell', 'grace.mitchell@southbridge.example', 'SR2 6DD'),
    ('Southbridge Retail', 'Southbridge Logistics', 'Noah', 'Foster', 'noah.foster@southbridge.example', 'SR2 6DD')
) AS contact_seed (ClientName, OrganisationName, FirstName, LastName, Email, Postcode)
INNER JOIN dbo.Clients AS client_lookup
    ON client_lookup.Name = contact_seed.ClientName
INNER JOIN dbo.Organisations AS organisation_lookup
    ON organisation_lookup.ClientId = client_lookup.ClientId
   AND organisation_lookup.Name = contact_seed.OrganisationName;
GO

INSERT INTO dbo.FormTypes (ClientId, Name, CreatedBy)
SELECT client_lookup.ClientId, form_type_seed.Name, 'seed-data'
FROM (VALUES
    ('Polaris Advisory', 'Sales form'),
    ('Southbridge Retail', 'Sales form')
) AS form_type_seed (ClientName, Name)
INNER JOIN dbo.Clients AS client_lookup
    ON client_lookup.Name = form_type_seed.ClientName;
GO

INSERT INTO dbo.FormMirrorRules
    (SourceClientId, SourceFormTypeId, TargetClientId, TargetFormTypeId, TargetPlaceholderOrganisationId, IsActive)
SELECT
    source_client.ClientId,
    source_form_type.FormTypeId,
    target_client.ClientId,
    target_form_type.FormTypeId,
    target_placeholder_organisation.OrganisationId,
    1
FROM dbo.Clients AS source_client
INNER JOIN dbo.FormTypes AS source_form_type
    ON source_form_type.ClientId = source_client.ClientId
   AND source_form_type.Name = 'Sales form'
INNER JOIN dbo.Clients AS target_client
    ON target_client.Name = 'Southbridge Retail'
INNER JOIN dbo.FormTypes AS target_form_type
    ON target_form_type.ClientId = target_client.ClientId
   AND target_form_type.Name = 'Sales form'
INNER JOIN dbo.Organisations AS target_placeholder_organisation
    ON target_placeholder_organisation.ClientId = target_client.ClientId
   AND target_placeholder_organisation.Name = 'Unassigned mirrored forms'
WHERE source_client.Name = 'Polaris Advisory';
GO

INSERT INTO dbo.Forms (ClientId, FormTypeId, OrganisationId, ContactId, Text1, Text2, DateTime1, DateTime2, CreatedBy)
SELECT
    client_lookup.ClientId,
    form_type_lookup.FormTypeId,
    organisation_lookup.OrganisationId,
    contact_lookup.ContactId,
    form_seed.Text1,
    form_seed.Text2,
    form_seed.DateTime1,
    form_seed.DateTime2,
    'seed-data'
FROM (VALUES
    ('Polaris Advisory', 'Sales form', 'Polaris Holdings', NULL, 'Initial sales qualification', 'Pipeline stage: Discovery', CAST('2026-03-01T09:00:00' AS datetime2(7)), CAST('2026-03-08T14:00:00' AS datetime2(7))),
    ('Polaris Advisory', 'Sales form', NULL, 'priya.shah@polaris.example', 'Follow-up call outcome', 'Interested in annual contract', CAST('2026-03-03T11:30:00' AS datetime2(7)), CAST('2026-03-10T16:15:00' AS datetime2(7))),
    ('Southbridge Retail', 'Sales form', 'Southbridge Manufacturing', NULL, 'Initial sales qualification', 'Pipeline stage: Proposal', CAST('2026-03-02T10:15:00' AS datetime2(7)), CAST('2026-03-09T15:45:00' AS datetime2(7))),
    ('Southbridge Retail', 'Sales form', NULL, 'noah.foster@southbridge.example', 'Follow-up call outcome', 'Requested pricing review', CAST('2026-03-04T13:00:00' AS datetime2(7)), CAST('2026-03-11T09:30:00' AS datetime2(7)))
) AS form_seed (ClientName, FormTypeName, OrganisationName, ContactEmail, Text1, Text2, DateTime1, DateTime2)
INNER JOIN dbo.Clients AS client_lookup
    ON client_lookup.Name = form_seed.ClientName
INNER JOIN dbo.FormTypes AS form_type_lookup
    ON form_type_lookup.ClientId = client_lookup.ClientId
   AND form_type_lookup.Name = form_seed.FormTypeName
LEFT JOIN dbo.Organisations AS organisation_lookup
    ON organisation_lookup.ClientId = client_lookup.ClientId
   AND organisation_lookup.Name = form_seed.OrganisationName
LEFT JOIN dbo.Contacts AS contact_lookup
    ON contact_lookup.ClientId = client_lookup.ClientId
   AND contact_lookup.Email = form_seed.ContactEmail;
GO

INSERT INTO dbo.Notes (ClientId, ContactId, Content, CreatedBy)
SELECT
    client_lookup.ClientId,
    contact_lookup.ContactId,
    note_seed.Content,
    'seed-data'
FROM (VALUES
    ('Polaris Advisory', 'alice.bennett@polaris.example', 'Initial introductory call completed. Interested in a quarterly review.'),
    ('Polaris Advisory', 'priya.shah@polaris.example', 'Requested a proposal summary before the next steering meeting.'),
    ('Southbridge Retail', 'sophie.turner@southbridge.example', 'Waiting for pricing confirmation from procurement.'),
    ('Southbridge Retail', 'noah.foster@southbridge.example', 'Confirmed follow-up workshop for next week.')
) AS note_seed (ClientName, ContactEmail, Content)
INNER JOIN dbo.Clients AS client_lookup
    ON client_lookup.Name = note_seed.ClientName
INNER JOIN dbo.Contacts AS contact_lookup
    ON contact_lookup.ClientId = client_lookup.ClientId
   AND contact_lookup.Email = note_seed.ContactEmail;
GO

COMMIT TRANSACTION;
GO
