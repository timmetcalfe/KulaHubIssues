-- Write your own SQL object definition here, and it'll be included in your package.
CREATE TRIGGER trg_FormInsert_Mirror
ON Forms
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Exit immediately if this insert is itself a mirror
    IF NOT EXISTS (
        SELECT 1 FROM inserted WHERE OriginalFormId IS NULL
    )
    RETURN;

    INSERT INTO Forms
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
        CreatedUtc,
        CreatedBy,
        OriginalFormId
    )
    SELECT
        m.TargetClientId,
        m.TargetFormTypeId,
        target_organisation.OrganisationId,
        NULL,
        i.Text1,
        i.Text2,
        i.Text3,
        i.DateTime1,
        i.DateTime2,
        GETUTCDATE(),
        i.CreatedBy,
        i.FormId
    FROM inserted i
    INNER JOIN FormMirrorRules m
        ON m.SourceClientId   = i.ClientId
        AND m.SourceFormTypeId = i.FormTypeId
        AND m.IsActive         = 1
    INNER JOIN Organisations target_organisation
        ON target_organisation.OrganisationId = m.TargetPlaceholderOrganisationId
        AND target_organisation.ClientId = m.TargetClientId
    WHERE i.OriginalFormId IS NULL;
END;