using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace StlTechRelGen.Db;

[Table("playsets")]
[Index("PdxId", IsUnique = true)]
public partial class Playset {
	[Key]
	[Column("id", TypeName = "char(36)")]
	public string Id { get; set; } = null!;

	[Column("name", TypeName = "varchar(255)")]
	public string Name { get; set; } = null!;

	[Column("isActive", TypeName = "boolean")]
	public bool? IsActive { get; set; }

	[Column("loadOrder", TypeName = "varchar(255)")]
	public string? LoadOrder { get; set; }

	[Column("pdxId", TypeName = "INT")]
	public int? PdxId { get; set; }

	[Column("pdxUserId", TypeName = "char(36)")]
	public string? PdxUserId { get; set; }

	[Column("createdOn", TypeName = "datetime")]
	public long CreatedOn { get; set; }

	[Column("updatedOn", TypeName = "datetime")]
	public long? UpdatedOn { get; set; }

	[Column("syncedOn", TypeName = "datetime")]
	public DateTime? SyncedOn { get; set; }

	[Column("lastServerChecksum")]
	public string? LastServerChecksum { get; set; }

	[Required]
	[Column("isRemoved", TypeName = "boolean")]
	public bool? IsRemoved { get; set; }

	[Required]
	[Column("hasNotApprovedChanges", TypeName = "boolean")]
	public bool? HasNotApprovedChanges { get; set; }

	[Column("syncState", TypeName = "varchar(255)")]
	public string? SyncState { get; set; }

	[Column("state")]
	public string State { get; set; } = null!;

	[Required]
	[Column("owned", TypeName = "boolean")]
	public bool? Owned { get; set; }

	[Column("author", TypeName = "varchar(255)")]
	public string Author { get; set; } = null!;

	[Column("subscribersCount")]
	public int SubscribersCount { get; set; }

	[Column("ratingsCount")]
	public int RatingsCount { get; set; }

	[Column("thumbnailFileUrl", TypeName = "varchar(255)")]
	public string? ThumbnailFileUrl { get; set; }

	[Column("description", TypeName = "varchar(255)")]
	public string? Description { get; set; }

	[Required]
	[Column("offDisk", TypeName = "boolean")]
	public bool? OffDisk { get; set; }
}
