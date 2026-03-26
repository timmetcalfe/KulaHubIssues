INSERT INTO [dbo].[Contacts]
(
    [ClientId],
    [OrganisationId],
    [FirstName],
    [LastName],
    [Email],
    [Postcode],
    [CreatedBy]
)
VALUES
(
    3,                          -- existing ClientId
    NULL,                       -- existing OrganisationId or NULL
    N'Jane',
    N'Doe',
    N'jane.doe@example.com',
    N'AB12 3CD',
    N'tim.metcalfe'
);