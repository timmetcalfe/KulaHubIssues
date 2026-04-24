CREATE TABLE [dbo].[Feedback]
(
    [FeedbackId] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(60) NULL,
    [Comments] NVARCHAR(MAX) NOT NULL,
    [CreatedUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_Feedback_CreatedUtc] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(100) NOT NULL,
    [ModifiedUtc] DATETIME2(7) NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [DeletedUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_Feedback] PRIMARY KEY CLUSTERED ([FeedbackId] ASC)
)
GO
