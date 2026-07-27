using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Noggin.SampleSite.Data;

public class UserAuthAccount
{
    [StringLength(64)]
    public required string Id { get; set; }

    [StringLength(32)]
    public required string Provider { get; set; }

    [StringLength(32)]
    public required string UserName { get; set; }
	}