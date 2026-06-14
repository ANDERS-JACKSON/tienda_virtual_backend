using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.ConfiguracionDB.CatalogoXqm;
using TiendaVirtual.Dominio.ConfiguracionDB.ConfiguracionXqm;
using TiendaVirtual.Dominio.ConfiguracionDB.PagoXqm;
using TiendaVirtual.Dominio.ConfiguracionDB.SeguridadXqm;
using TiendaVirtual.Dominio.ConfiguracionDB.SoporteXqm;
using TiendaVirtual.Dominio.ConfiguracionDB.VendedorXqm;
using TiendaVirtual.Dominio.ConfiguracionDB.VentaXqm;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Dominio.Modelo.ConfiguracionXqm;
using TiendaVirtual.Dominio.Modelo.PagoXqm;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Dominio.Modelo.VentaXqm;

namespace TiendaVirtual.Dominio
{
    public class TiendaVirtualDbContext : DbContext
    {
        public TiendaVirtualDbContext(DbContextOptions<TiendaVirtualDbContext> options)
            : base(options)
        {
        }

        #region Seguridad
        //----------------------------------------------------------------------
        // Seguridad
        //----------------------------------------------------------------------
        public DbSet<Persona> Personas { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Rol> Roles { get; set; } = null!;
        public DbSet<UsuarioRol> UsuarioRoles { get; set; } = null!;
        public DbSet<Direccion> Direcciones { get; set; } = null!;
        public DbSet<TokenRefresco> TokensRefresco { get; set; } = null!;
        //----------------------------------------------------------------------
        #endregion

        #region Vendedor
        //----------------------------------------------------------------------
        // Vendedor
        //----------------------------------------------------------------------
        public DbSet<Vendedor> Vendedores { get; set; } = null!;
        public DbSet<SolicitudVerificacion> SolicitudesVerificacion { get; set; } = null!;
        public DbSet<CuentaBancaria> CuentasBancarias { get; set; } = null!;
        public DbSet<Plan> Planes { get; set; } = null!;
        public DbSet<Cupon> Cupones { get; set; } = null!;
        public DbSet<Suscripcion> Suscripciones { get; set; } = null!;
        //----------------------------------------------------------------------
        #endregion

        #region Catalogo
        //----------------------------------------------------------------------
        // Catalogo
        //----------------------------------------------------------------------
        public DbSet<Categoria> Categorias { get; set; } = null!;
        public DbSet<Producto> Productos { get; set; } = null!;
        public DbSet<VarianteProducto> VariantesProducto { get; set; } = null!;
        public DbSet<ImagenProducto> ImagenesProducto { get; set; } = null!;
        public DbSet<Stock> Stocks { get; set; } = null!;
        public DbSet<Oferta> Ofertas { get; set; } = null!;
        public DbSet<Favorito> Favoritos { get; set; } = null!;
        public DbSet<ProductoDestacado> ProductosDestacados { get; set; } = null!;
        //----------------------------------------------------------------------
        #endregion

        #region Venta
        //----------------------------------------------------------------------
        // Venta
        //----------------------------------------------------------------------
        public DbSet<Carrito> Carritos { get; set; } = null!;
        public DbSet<ItemCarrito> ItemsCarrito { get; set; } = null!;
        public DbSet<MetodoEnvio> MetodosEnvio { get; set; } = null!;
        public DbSet<Orden> Ordenes { get; set; } = null!;
        public DbSet<Suborden> Subordenes { get; set; } = null!;
        public DbSet<ItemOrden> ItemsOrden { get; set; } = null!;
        public DbSet<Envio> Envios { get; set; } = null!;
        //----------------------------------------------------------------------
        #endregion

        #region Pago
        //----------------------------------------------------------------------
        // Pago
        //----------------------------------------------------------------------
        public DbSet<Transaccion> Transacciones { get; set; } = null!;
        public DbSet<Billetera> Billeteras { get; set; } = null!;
        public DbSet<MovimientoBilletera> MovimientosBilletera { get; set; } = null!;
        public DbSet<Retiro> Retiros { get; set; } = null!;
        //----------------------------------------------------------------------
        #endregion

        #region Soporte
        //----------------------------------------------------------------------
        // Soporte
        //----------------------------------------------------------------------
        public DbSet<ResenaProducto> ResenasProducto { get; set; } = null!;
        public DbSet<ResenaVendedor> ResenasVendedor { get; set; } = null!;
        public DbSet<Reclamo> Reclamos { get; set; } = null!;
        public DbSet<MensajeReclamo> MensajesReclamo { get; set; } = null!;
        public DbSet<Notificacion> Notificaciones { get; set; } = null!;
        public DbSet<MensajeContacto> MensajesContacto { get; set; } = null!;
        //----------------------------------------------------------------------
        #endregion

        #region Configuracion
        //----------------------------------------------------------------------
        // Configuracion
        //----------------------------------------------------------------------
        public DbSet<Configuracion> Configuracion { get; set; } = null!;
        public DbSet<Correo> Correo { get; set; } = null!;
        //----------------------------------------------------------------------
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region Seguridad
            modelBuilder.ApplyConfiguration(new PersonaDB());
            modelBuilder.ApplyConfiguration(new UsuarioDB());
            modelBuilder.ApplyConfiguration(new RolDB());
            modelBuilder.ApplyConfiguration(new UsuarioRolDB());
            modelBuilder.ApplyConfiguration(new DireccionDB());
            modelBuilder.ApplyConfiguration(new TokenRefrescoDB());
            #endregion

            #region Vendedor
            modelBuilder.ApplyConfiguration(new VendedorDB());
            modelBuilder.ApplyConfiguration(new SolicitudVerificacionDB());
            modelBuilder.ApplyConfiguration(new CuentaBancariaDB());
            modelBuilder.ApplyConfiguration(new PlanDB());
            modelBuilder.ApplyConfiguration(new CuponDB());
            modelBuilder.ApplyConfiguration(new SuscripcionDB());
            #endregion

            #region Catalogo
            modelBuilder.ApplyConfiguration(new CategoriaDB());
            modelBuilder.ApplyConfiguration(new ProductoDB());
            modelBuilder.ApplyConfiguration(new VarianteProductoDB());
            modelBuilder.ApplyConfiguration(new ImagenProductoDB());
            modelBuilder.ApplyConfiguration(new StockDB());
            modelBuilder.ApplyConfiguration(new OfertaDB());
            modelBuilder.ApplyConfiguration(new FavoritoDB());
            modelBuilder.ApplyConfiguration(new ProductoDestacadoDB());
            #endregion

            #region Venta
            modelBuilder.ApplyConfiguration(new CarritoDB());
            modelBuilder.ApplyConfiguration(new ItemCarritoDB());
            modelBuilder.ApplyConfiguration(new MetodoEnvioDB());
            modelBuilder.ApplyConfiguration(new OrdenDB());
            modelBuilder.ApplyConfiguration(new SubordenDB());
            modelBuilder.ApplyConfiguration(new ItemOrdenDB());
            modelBuilder.ApplyConfiguration(new EnvioDB());
            #endregion

            #region Pago
            modelBuilder.ApplyConfiguration(new TransaccionDB());
            modelBuilder.ApplyConfiguration(new BilleteraDB());
            modelBuilder.ApplyConfiguration(new MovimientoBilleteraDB());
            modelBuilder.ApplyConfiguration(new RetiroDB());
            #endregion

            #region Soporte
            modelBuilder.ApplyConfiguration(new ResenaProductoDB());
            modelBuilder.ApplyConfiguration(new ResenaVendedorDB());
            modelBuilder.ApplyConfiguration(new ReclamoDB());
            modelBuilder.ApplyConfiguration(new MensajeReclamoDB());
            modelBuilder.ApplyConfiguration(new NotificacionDB());
            modelBuilder.ApplyConfiguration(new MensajeContactoDB());
            #endregion

            #region Configuracion
            modelBuilder.ApplyConfiguration(new ConfiguracionGeneralDB());
            modelBuilder.ApplyConfiguration(new CorreoDB());
            #endregion

            base.OnModelCreating(modelBuilder);
        }
    }
}
