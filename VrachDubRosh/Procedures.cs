//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace VrachDubRosh
{
    using System;
    using System.Collections.Generic;
    
    public partial class Procedures
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Procedures()
        {
            this.ProcedureAppointments = new HashSet<ProcedureAppointments>();
        }
    
        public int ProcedureID { get; set; }
        public Nullable<int> DoctorID { get; set; }
        public string ProcedureName { get; set; }
        public Nullable<int> Duration { get; set; }
    
        public virtual Doctors Doctors { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ProcedureAppointments> ProcedureAppointments { get; set; }
    }
}
