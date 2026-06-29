using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace StlTechRelGen.Db;

internal sealed partial class LauncherV2DbContext(
	Config.GameConfig config
) : DbContext {
	public DbSet<Mod> Mods { get; private set; }

	public DbSet<Playset> Playsets { get; private set; }

	public DbSet<PlaysetsMod> PlaysetsMods { get; private set; }

	protected override void OnConfiguring(
		DbContextOptionsBuilder optionsBuilder
	) => optionsBuilder
		.UseModel(LauncherV2DbContextModel.Instance)
		.UseSqlite(
			new SqliteConnectionStringBuilder() {
				DataSource = Path.Combine(config.DocumentPath, "launcher-v2.sqlite"),
				Mode = SqliteOpenMode.ReadOnly
			}.ConnectionString
		);

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.Entity<Mod>(entity => {
			entity.Property(e => e.Cause).HasDefaultValueSql("null");
			entity.Property(e => e.IsMetadataApplied).HasDefaultValueSql("'0'");
			entity.Property(e => e.KeepLatest).HasDefaultValueSql("'1'");
			entity.Property(e => e.MetadataStatus).HasDefaultValue("not_applied");
			entity.Property(e => e.Tags).HasDefaultValue("[]");
			entity.Property(e => e.UserVersion).HasDefaultValueSql("null");
		});

		modelBuilder.Entity<Playset>(entity => {
			entity.Property(e => e.Author).HasDefaultValue("");
			entity.Property(e => e.Description).HasDefaultValue("");
			entity.Property(e => e.HasNotApprovedChanges).HasDefaultValueSql("'0'");
			entity.Property(e => e.IsRemoved).HasDefaultValueSql("false");
			entity.Property(e => e.OffDisk).HasDefaultValueSql("'0'");
			entity.Property(e => e.Owned).HasDefaultValueSql("'1'");
			entity.Property(e => e.RatingsCount).HasDefaultValueSql("'0'");
			entity.Property(e => e.State).HasDefaultValue("private");
			entity.Property(e => e.SubscribersCount).HasDefaultValueSql("'0'");
		});

		modelBuilder.Entity<PlaysetsMod>(entity =>
			entity.Property(e => e.Enabled).HasDefaultValueSql("'1'")
		);

		OnModelCreatingPartial(modelBuilder);
	}

	partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
