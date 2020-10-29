
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Netflix.Data
{
    public class DataContext : IdentityDbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<Capitulos> Capitulos{get;set;}
        public DbSet<Categorias> Categorias{get;set;}
        public DbSet<Historial> Historial{get;set;}
        public DbSet<Peliculas> Peliculas{get;set;}
        public DbSet<Portada> Portada{get;set;}
        public DbSet<Series> Series{get;set;}

        
    }
}