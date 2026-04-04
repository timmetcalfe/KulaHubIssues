INSERT INTO dbo.Forms
(
    ClientId,
    FormTypeId,
    OrganisationId,
    ContactId,
    Text1,
    Text2,
    Text3,
    DateTime1,
    DateTime2,
    CreatedBy,
    OriginalFormId
)
VALUES
(
    3,
    1,
    2,
    NULL,
    'Trigger test form',
    'This row should be mirrored to Dealer',
    'Inserted manually for trigger test',
    CAST('2026-03-12T10:00:00' AS datetime2(7)),
    CAST('2026-03-19T10:00:00' AS datetime2(7)),
    'manual-trigger-test',
    NULL
);