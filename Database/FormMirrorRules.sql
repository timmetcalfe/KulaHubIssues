CREATE TABLE [dbo].[FormMirrorRules]
(
  [FormMirrorRuleId] INT IDENTITY PRIMARY KEY,
  [SourceClientId] INT NOT NULL,
  [SourceFormTypeId] INT NOT NULL,
  [TargetClientId] INT NOT NULL,
  [TargetFormTypeId] INT NOT NULL,
  [TargetPlaceholderOrganisationId] INT NOT NULL,
  [IsActive] BIT NOT NULL DEFAULT 1,
  CONSTRAINT [FK_FormMirrorRules_TargetPlaceholderOrganisation]
      FOREIGN KEY ([TargetPlaceholderOrganisationId]) REFERENCES [dbo].[Organisations] ([OrganisationId])
)

GO

CREATE INDEX [IX_FormMirrorRules_TargetPlaceholderOrganisationId]
    ON [dbo].[FormMirrorRules] ([TargetPlaceholderOrganisationId]);
