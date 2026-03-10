using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyKhoLinhKienPC.Models;

[ModelMetadataType(typeof(VaiTroMetadata))]
public partial class VaiTro { }

public class VaiTroMetadata
{
    [Required(ErrorMessage = "Vui lòng nhập tên vai trò!")]
    [MaxLength(50, ErrorMessage = "Tên vai trò tối đa 50 ký tự!")]
    public string TenVaiTro { get; set; }
}