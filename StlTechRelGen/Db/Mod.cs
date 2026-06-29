using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StlTechRelGen.Db;

[Table("mods")]
public partial class Mod {
	[Key]
	[Column("id", TypeName = "char(36)")]
	public string Id { get; set; } = null!;

	[Column("pdxId", TypeName = "varchar(255)")]
	public string? PdxId { get; set; }

	[Column("steamId", TypeName = "varchar(255)")]
	public string? SteamId { get; set; }

	[Column("gameRegistryId")]
	public string? GameRegistryId { get; set; }

	[Column("name", TypeName = "varchar(255)")]
	public string? Name { get; set; }

	[Column("displayName", TypeName = "varchar(255)")]
	public string? DisplayName { get; set; }

	[Column("descriptionDeprecated")]
	public string? DescriptionDeprecated { get; set; }

	[Column("thumbnailUrl")]
	public string? ThumbnailUrl { get; set; }

	[Column("thumbnailPath")]
	public string? ThumbnailPath { get; set; }

	[Column("version", TypeName = "varchar(255)")]
	public string? Version { get; set; }

	[Column("tags", TypeName = "json")]
	public string? Tags { get; set; }

	[Column("requiredVersion", TypeName = "varchar(255)")]
	public string? RequiredVersion { get; set; }

	[Column("arch")]
	public string? Arch { get; set; }

	[Column("os")]
	public string? Os { get; set; }

	[Column("repositoryPath")]
	public string? RepositoryPath { get; set; }

	[Column("dirPath")]
	public string? DirPath { get; set; }

	[Column("archivePath")]
	public string? ArchivePath { get; set; }

	[Column("status")]
	public string Status { get; set; } = null!;

	[Column("source")]
	public string Source { get; set; } = null!;

	[Column("cause")]
	public string? Cause { get; set; }

	[Column("timeUpdated")]
	public int? TimeUpdated { get; set; }

	[Column("isNew", TypeName = "boolean")]
	public bool? IsNew { get; set; }

	[Column("createdDate", TypeName = "datetime")]
	public int? CreatedDate { get; set; }

	[Column("subscribedDate", TypeName = "datetime")]
	public int? SubscribedDate { get; set; }

	[Column("size")]
	public long? Size { get; set; }

	[Column("metadataId", TypeName = "varchar(255)")]
	public string? MetadataId { get; set; }

	[Column("remotePdxId", TypeName = "varchar(255)")]
	public string? RemotePdxId { get; set; }

	[Column("remoteSteamId", TypeName = "varchar(255)")]
	public string? RemoteSteamId { get; set; }

	[Column("metadataVersion", TypeName = "varchar(255)")]
	public string? MetadataVersion { get; set; }

	[Required]
	[Column("isMetadataApplied", TypeName = "boolean")]
	public bool? IsMetadataApplied { get; set; }

	[Column("metadataStatus")]
	public string MetadataStatus { get; set; } = null!;

	[Column("metadataGameId", TypeName = "varchar(255)")]
	public string? MetadataGameId { get; set; }

	[Column("descriptionPdx", TypeName = "varchar(255)")]
	public string? DescriptionPdx { get; set; }

	[Column("descriptionSteam", TypeName = "varchar(255)")]
	public string? DescriptionSteam { get; set; }

	[Column("shortDescriptionPdx", TypeName = "varchar(255)")]
	public string? ShortDescriptionPdx { get; set; }

	[Required]
	[Column("keepLatest", TypeName = "boolean")]
	public bool? KeepLatest { get; set; }

	[Column("userVersion", TypeName = "boolean")]
	public bool? UserVersion { get; set; }

	[Column("remotePdxUserId", TypeName = "varchar(255)")]
	public string? RemotePdxUserId { get; set; }

	[Column("remoteSteamUserId", TypeName = "varchar(255)")]
	public string? RemoteSteamUserId { get; set; }
}
