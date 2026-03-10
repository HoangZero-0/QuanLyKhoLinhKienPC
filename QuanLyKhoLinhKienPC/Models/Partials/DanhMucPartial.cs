using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyKhoLinhKienPC.Models;

[ModelMetadataType(typeof(DanhMucMetadata))]
public partial class DanhMuc { }

public class DanhMucMetadata
{
    [Required(ErrorMessage = "Vui lòng nhập tên danh mục!")]
    [MaxLength(100, ErrorMessage = "Tên danh mục tối đa 100 ký tự!")]
    public string TenDanhMuc { get; set; }
}