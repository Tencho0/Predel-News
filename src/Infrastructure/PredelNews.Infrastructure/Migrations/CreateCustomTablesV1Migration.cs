using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseModelDefinitions;

namespace PredelNews.Infrastructure.Migrations;

public class CreateCustomTablesV1Migration : AsyncMigrationBase
{
    public CreateCustomTablesV1Migration(IMigrationContext context) : base(context) { }

    protected override async Task MigrateAsync()
    {
        Logger.LogInformation("Running PredelNews v1 migration — creating custom tables");

        CreateCommentsTable();
        CreateCommentAuditLogTable();
        CreatePollsTable();
        CreatePollOptionsTable();
        CreateAdSlotsTable();
        CreateEmailSubscribersTable();
        CreateContactSubmissionsTable();
        CreateAuditLogTable();
        SeedAdSlots();

        await Task.CompletedTask;
    }

    private void CreateCommentsTable()
    {
        if (TableExists("pn_comments"))
            return;

        Create.Table<CommentSchema>().Do();

        Execute.Sql(@"
            CREATE NONCLUSTERED INDEX [IX_pn_comments_article_created]
            ON [pn_comments] ([article_id], [created_at])
            WHERE [is_deleted] = 0
        ").Do();
    }

    private void CreateCommentAuditLogTable()
    {
        if (TableExists("pn_comment_audit_log"))
            return;

        Create.Table<CommentAuditLogSchema>().Do();
    }

    private void CreatePollsTable()
    {
        if (TableExists("pn_polls"))
            return;

        Create.Table<PollSchema>().Do();

        Execute.Sql(@"
            CREATE UNIQUE NONCLUSTERED INDEX [IX_pn_polls_single_active]
            ON [pn_polls] ([is_active])
            WHERE [is_active] = 1
        ").Do();
    }

    private void CreatePollOptionsTable()
    {
        if (TableExists("pn_poll_options"))
            return;

        Create.Table<PollOptionSchema>().Do();
    }

    private void CreateAdSlotsTable()
    {
        if (TableExists("pn_ad_slots"))
            return;

        Create.Table<AdSlotSchema>().Do();

        Execute.Sql(@"
            ALTER TABLE [pn_ad_slots]
            ADD CONSTRAINT [CK_pn_ad_slots_mode] CHECK ([mode] IN ('adsense', 'direct'))
        ").Do();
    }

    private void CreateEmailSubscribersTable()
    {
        if (TableExists("pn_email_subscribers"))
            return;

        Create.Table<EmailSubscriberSchema>().Do();
    }

    private void CreateContactSubmissionsTable()
    {
        if (TableExists("pn_contact_submissions"))
            return;

        Create.Table<ContactSubmissionSchema>().Do();
    }

    private void CreateAuditLogTable()
    {
        if (TableExists("pn_audit_log"))
            return;

        Create.Table<AuditLogSchema>().Do();

        Execute.Sql(@"
            CREATE NONCLUSTERED INDEX [IX_pn_audit_log_event_created]
            ON [pn_audit_log] ([event_type], [created_at])
        ").Do();

        Execute.Sql(@"
            CREATE NONCLUSTERED INDEX [IX_pn_audit_log_created_desc]
            ON [pn_audit_log] ([created_at] DESC)
        ").Do();
    }

    private void SeedAdSlots()
    {
        var count = Database.ExecuteScalar<int>("SELECT COUNT(*) FROM [pn_ad_slots]");
        if (count > 0)
            return;

        var slots = new[]
        {
            ("header-leaderboard", "Header Leaderboard (728x90)"),
            ("sidebar-top", "Sidebar Top (300x250)"),
            ("sidebar-bottom", "Sidebar Bottom (300x250)"),
            ("article-inline", "Article Inline (728x90)"),
            ("footer-banner", "Footer Banner (728x90)"),
            ("mobile-sticky", "Mobile Sticky (320x50)"),
        };

        foreach (var (slotId, slotName) in slots)
        {
            Database.Execute(
                "INSERT INTO [pn_ad_slots] ([slot_id], [slot_name], [mode], [updated_at]) VALUES (@0, @1, @2, GETUTCDATE())",
                slotId, slotName, "adsense");
        }

        Logger.LogInformation("Seeded {Count} ad slots", slots.Length);
    }

    // ── Schema DTOs ──────────────────────────────────────────────────────

    [TableName("pn_comments")]
    [PrimaryKey("id", AutoIncrement = true)]
    [ExplicitColumns]
    internal class CommentSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        [Column("id")]
        public int Id { get; set; }

        [Column("article_id")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public int ArticleId { get; set; }

        [Column("display_name")]
        [Length(200)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string DisplayName { get; set; } = null!;

        [Column("comment_text")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string CommentText { get; set; } = null!;

        [Column("ip_address")]
        [Length(45)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string IpAddress { get; set; } = null!;

        [Column("created_at")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        [Constraint(Default = SystemMethods.CurrentDateTime)]
        public DateTime CreatedAt { get; set; }

        [Column("is_deleted")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public bool IsDeleted { get; set; }

        [Column("is_held")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public bool IsHeld { get; set; }

        [Column("held_reason")]
        [Length(500)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? HeldReason { get; set; }

        [Column("parent_comment_id")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [ForeignKey(typeof(CommentSchema), Name = "FK_pn_comments_parent")]
        public int? ParentCommentId { get; set; }
    }

    [TableName("pn_comment_audit_log")]
    [PrimaryKey("id", AutoIncrement = true)]
    [ExplicitColumns]
    internal class CommentAuditLogSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        [Column("id")]
        public int Id { get; set; }

        [Column("comment_id")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        [ForeignKey(typeof(CommentSchema), Name = "FK_pn_comment_audit_log_comment")]
        public int CommentId { get; set; }

        [Column("action")]
        [Length(50)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string Action { get; set; } = null!;

        [Column("acting_user_id")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? ActingUserId { get; set; }

        [Column("acting_username")]
        [Length(255)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? ActingUsername { get; set; }

        [Column("created_at")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        [Constraint(Default = SystemMethods.CurrentDateTime)]
        public DateTime CreatedAt { get; set; }

        [Column("original_text")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? OriginalText { get; set; }
    }

    [TableName("pn_polls")]
    [PrimaryKey("id", AutoIncrement = true)]
    [ExplicitColumns]
    internal class PollSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        [Column("id")]
        public int Id { get; set; }

        [Column("question")]
        [Length(500)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string Question { get; set; } = null!;

        [Column("is_active")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public bool IsActive { get; set; }

        [Column("created_at")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        [Constraint(Default = SystemMethods.CurrentDateTime)]
        public DateTime CreatedAt { get; set; }

        [Column("closed_at")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? ClosedAt { get; set; }
    }

    [TableName("pn_poll_options")]
    [PrimaryKey("id", AutoIncrement = true)]
    [ExplicitColumns]
    internal class PollOptionSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        [Column("id")]
        public int Id { get; set; }

        [Column("poll_id")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        [ForeignKey(typeof(PollSchema), Name = "FK_pn_poll_options_poll")]
        public int PollId { get; set; }

        [Column("option_text")]
        [Length(300)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string OptionText { get; set; } = null!;

        [Column("vote_count")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public int VoteCount { get; set; }
    }

    [TableName("pn_ad_slots")]
    [PrimaryKey("id", AutoIncrement = true)]
    [ExplicitColumns]
    internal class AdSlotSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        [Column("id")]
        public int Id { get; set; }

        [Column("slot_id")]
        [Length(100)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        [Index(IndexTypes.UniqueNonClustered, Name = "IX_pn_ad_slots_slot_id")]
        public string SlotId { get; set; } = null!;

        [Column("slot_name")]
        [Length(200)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string SlotName { get; set; } = null!;

        [Column("mode")]
        [Length(20)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string Mode { get; set; } = null!;

        [Column("adsense_code")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? AdsenseCode { get; set; }

        [Column("banner_image_url")]
        [Length(500)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? BannerImageUrl { get; set; }

        [Column("banner_dest_url")]
        [Length(500)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? BannerDestUrl { get; set; }

        [Column("banner_alt_text")]
        [Length(300)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? BannerAltText { get; set; }

        [Column("start_date")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? StartDate { get; set; }

        [Column("end_date")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? EndDate { get; set; }

        [Column("updated_at")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        [Constraint(Default = SystemMethods.CurrentDateTime)]
        public DateTime UpdatedAt { get; set; }
    }

    [TableName("pn_email_subscribers")]
    [PrimaryKey("id", AutoIncrement = true)]
    [ExplicitColumns]
    internal class EmailSubscriberSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        [Column("id")]
        public int Id { get; set; }

        [Column("email")]
        [Length(255)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        [Index(IndexTypes.UniqueNonClustered, Name = "IX_pn_email_subscribers_email")]
        public string Email { get; set; } = null!;

        [Column("signed_up_at")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        [Constraint(Default = SystemMethods.CurrentDateTime)]
        public DateTime SignedUpAt { get; set; }

        [Column("consent_flag")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public bool ConsentFlag { get; set; }
    }

    [TableName("pn_contact_submissions")]
    [PrimaryKey("id", AutoIncrement = true)]
    [ExplicitColumns]
    internal class ContactSubmissionSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [Length(200)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string Name { get; set; } = null!;

        [Column("email")]
        [Length(255)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string Email { get; set; } = null!;

        [Column("subject")]
        [Length(300)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string Subject { get; set; } = null!;

        [Column("message")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string Message { get; set; } = null!;

        [Column("submitted_at")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        [Constraint(Default = SystemMethods.CurrentDateTime)]
        [Index(IndexTypes.NonClustered, Name = "IX_pn_contact_submissions_submitted")]
        public DateTime SubmittedAt { get; set; }

        [Column("ip_address")]
        [Length(45)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string IpAddress { get; set; } = null!;
    }

    [TableName("pn_audit_log")]
    [PrimaryKey("id", AutoIncrement = true)]
    [ExplicitColumns]
    internal class AuditLogSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        [Column("id")]
        public int Id { get; set; }

        [Column("event_type")]
        [Length(100)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string EventType { get; set; } = null!;

        [Column("acting_user_id")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? ActingUserId { get; set; }

        [Column("acting_username")]
        [Length(255)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? ActingUsername { get; set; }

        [Column("created_at")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        [Constraint(Default = SystemMethods.CurrentDateTime)]
        public DateTime CreatedAt { get; set; }

        [Column("entity_type")]
        [Length(100)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? EntityType { get; set; }

        [Column("entity_id")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? EntityId { get; set; }

        [Column("previous_value")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? PreviousValue { get; set; }

        [Column("new_value")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? NewValue { get; set; }

        [Column("notes")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? Notes { get; set; }
    }
}
