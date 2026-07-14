using KulaHub.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KulaHub.Data;

public sealed class KulaHubDbContext(DbContextOptions<KulaHubDbContext> options) : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Organisation> Organisations => Set<Organisation>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<FormType> FormTypes => Set<FormType>();
    public DbSet<Form> Forms => Set<Form>();
    public DbSet<IntegrationInboxEntry> IntegrationInbox => Set<IntegrationInboxEntry>();
    public DbSet<IntegrationDispatchEntry> IntegrationDispatch => Set<IntegrationDispatchEntry>();
    public DbSet<Feedback> Feedback => Set<Feedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("Clients", "dbo");
            entity.HasKey(x => x.ClientId);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Postcode).HasMaxLength(12);
            entity.Property(x => x.CreatedBy).HasMaxLength(100);
            entity.Property(x => x.ModifiedBy).HasMaxLength(100);
        });

        modelBuilder.Entity<Organisation>(entity =>
        {
            entity.ToTable("Organisations", "dbo");
            entity.HasKey(x => x.OrganisationId);
            entity.HasIndex(x => x.ClientId).HasDatabaseName("IX_Organisations_ClientId");
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.Postcode).HasMaxLength(12);
            entity.Property(x => x.CreatedBy).HasMaxLength(100);
            entity.Property(x => x.ModifiedBy).HasMaxLength(100);
            entity.HasOne(x => x.Client)
                .WithMany(x => x.Organisations)
                .HasForeignKey(x => x.ClientId);
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.ToTable("Contacts", "dbo");
            entity.HasKey(x => x.ContactId);
            entity.HasIndex(x => x.ClientId).HasDatabaseName("IX_Contacts_ClientId");
            entity.HasIndex(x => new { x.ClientId, x.SourceContactId }).HasDatabaseName("IX_Contacts_SourceContactId");
            entity.HasIndex(x => new { x.ClientId, x.Email }).HasDatabaseName("IX_Contacts_Email");
            entity.HasIndex(x => x.OrganisationId).HasDatabaseName("IX_Contacts_OrganisationId");
            entity.Property(x => x.FirstName).HasMaxLength(50);
            entity.Property(x => x.LastName).HasMaxLength(50);
            entity.Property(x => x.Email).HasMaxLength(60);
            entity.Property(x => x.Postcode).HasMaxLength(12);
            entity.Property(x => x.CreatedBy).HasMaxLength(100);
            entity.Property(x => x.ModifiedBy).HasMaxLength(100);
            entity.HasOne(x => x.Client)
                .WithMany(x => x.Contacts)
                .HasForeignKey(x => x.ClientId);
            entity.HasOne(x => x.Organisation)
                .WithMany(x => x.Contacts)
                .HasForeignKey(x => x.OrganisationId);
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.ToTable("Notes", "dbo");
            entity.HasKey(x => x.NoteId);
            entity.HasIndex(x => x.ClientId).HasDatabaseName("IX_Notes_ClientId");
            entity.HasIndex(x => x.ContactId).HasDatabaseName("IX_Notes_ContactId");
            entity.Property(x => x.CreatedBy).HasMaxLength(100);
            entity.Property(x => x.ModifiedBy).HasMaxLength(100);
            entity.HasOne(x => x.Client)
                .WithMany(x => x.Notes)
                .HasForeignKey(x => x.ClientId);
            entity.HasOne(x => x.Contact)
                .WithMany(x => x.Notes)
                .HasForeignKey(x => x.ContactId);
        });

        modelBuilder.Entity<FormType>(entity =>
        {
            entity.ToTable("FormTypes", "dbo");
            entity.HasKey(x => x.FormTypeId);
            entity.HasIndex(x => x.ClientId).HasDatabaseName("IX_FormTypes_ClientId");
            entity.Property(x => x.CreatedBy).HasMaxLength(100);
            entity.Property(x => x.ModifiedBy).HasMaxLength(100);
            entity.HasOne(x => x.Client)
                .WithMany(x => x.FormTypes)
                .HasForeignKey(x => x.ClientId);
        });

        modelBuilder.Entity<Form>(entity =>
        {
            entity.ToTable("Forms", "dbo");
            entity.HasKey(x => x.FormId);
            entity.HasIndex(x => x.ClientId).HasDatabaseName("IX_Forms_ClientId");
            entity.HasIndex(x => x.FormTypeId).HasDatabaseName("IX_Forms_FormTypeId");
            entity.HasIndex(x => x.OrganisationId).HasDatabaseName("IX_Forms_OrganisationId");
            entity.HasIndex(x => x.ContactId).HasDatabaseName("IX_Forms_ContactId");
            entity.Property(x => x.CreatedBy).HasMaxLength(100);
            entity.Property(x => x.ModifiedBy).HasMaxLength(100);
            entity.HasOne(x => x.Client)
                .WithMany(x => x.Forms)
                .HasForeignKey(x => x.ClientId);
            entity.HasOne(x => x.FormType)
                .WithMany(x => x.Forms)
                .HasForeignKey(x => x.FormTypeId);
            entity.HasOne(x => x.Organisation)
                .WithMany(x => x.Forms)
                .HasForeignKey(x => x.OrganisationId);
            entity.HasOne(x => x.Contact)
                .WithMany(x => x.Forms)
                .HasForeignKey(x => x.ContactId);
        });

        modelBuilder.Entity<IntegrationInboxEntry>(entity =>
        {
            entity.ToTable("IntegrationInbox", "dbo");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ClientId).HasDatabaseName("IX_IntegrationInbox_ClientId");
            entity.HasIndex(x => x.SourceSystemKey).HasDatabaseName("IX_IntegrationInbox_SourceSystemKey");
            entity.HasIndex(x => new { x.ProcessedUtc, x.ReceivedUtc }).HasDatabaseName("IX_IntegrationInbox_Unprocessed");
            entity.Property(x => x.CorrelationId).HasMaxLength(32);
            entity.Property(x => x.TraceParent).HasMaxLength(55);
            entity.Property(x => x.OriginType).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.SourceSystemKey).HasMaxLength(100);
            entity.Property(x => x.EntityType).HasMaxLength(100);
            entity.Property(x => x.EventType).HasMaxLength(100);
            entity.Property(x => x.ChangeType).HasMaxLength(50);
            entity.Property(x => x.ExternalEntityId).HasMaxLength(100);
        });

        modelBuilder.Entity<IntegrationDispatchEntry>(entity =>
        {
            entity.ToTable("IntegrationDispatch", "dbo");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ClientId).HasDatabaseName("IX_IntegrationDispatch_ClientId");
            entity.HasIndex(x => x.IntegrationInboxId).HasDatabaseName("IX_IntegrationDispatch_InboxId");
            entity.HasIndex(x => x.QueueKey).HasDatabaseName("IX_IntegrationDispatch_QueueKey");
            entity.HasIndex(x => x.SourceSystemKey).HasDatabaseName("IX_IntegrationDispatch_SourceSystemKey");
            entity.HasIndex(x => new { x.ProcessedUtc, x.ReceivedUtc }).HasDatabaseName("IX_IntegrationDispatch_Unprocessed");
            entity.HasIndex(x => new { x.DispatchedUtc, x.ReceivedUtc }).HasDatabaseName("IX_IntegrationDispatch_Undispatched");
            entity.Property(x => x.CorrelationId).HasMaxLength(32);
            entity.Property(x => x.TraceParent).HasMaxLength(55);
            entity.Property(x => x.Disposition).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.OriginType).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.SourceSystemKey).HasMaxLength(100);
            entity.Property(x => x.QueueKey).HasMaxLength(100);
            entity.Property(x => x.EntityType).HasMaxLength(100);
            entity.Property(x => x.EventType).HasMaxLength(100);
            entity.Property(x => x.ChangeType).HasMaxLength(50);
            entity.Property(x => x.ExternalEntityId).HasMaxLength(100);
            entity.Property(x => x.DispatchTarget).HasMaxLength(200);
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("Feedback", "dbo");
            entity.HasKey(x => x.FeedbackId);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CreatedBy).HasMaxLength(100);
            entity.Property(x => x.ModifiedBy).HasMaxLength(100);
        });
    }
}