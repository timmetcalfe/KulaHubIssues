CREATE TABLE [dbo].[FormMirrorRules]
(
  [FormMirrorRulesId] INT IDENTITY PRIMARY KEY,
  [SourceClientId] INT NOT NULL,
  [SourceFormTypeId] INT NOT NULL,
  [TargetClientId] INT NOT NULL,
  [TargetFormTypeId] INT NOT NULL,
  [IsActive] BIT NOT NULL DEFAULT 1
)
