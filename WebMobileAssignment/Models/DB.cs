using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebMobileAssignment.Models;

#nullable disable warnings

public class DB(DbContextOptions options) : DbContext(options)
{
    // DB Sets
}
