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

    INSERT INTO Forms (ClientId, FormTypeId, Text1, Text2, CreatedUtc, OriginalFormId)
    SELECT
        m.TargetClientId,
        m.TargetFormTypeId,
        i.Text1,
        i.Text2,
        GETUTCDATE(),
        i.FormId
    FROM inserted i
    INNER JOIN FormMirrorRules m
        ON m.SourceClientId   = i.ClientId
        AND m.SourceFormTypeId = i.FormTypeId
        AND m.IsActive         = 1;
END;