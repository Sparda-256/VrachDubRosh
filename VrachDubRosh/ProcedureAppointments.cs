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
    
    public partial class ProcedureAppointments
    {
        public int AppointmentID { get; set; }
        public Nullable<int> PatientID { get; set; }
        public Nullable<int> DoctorID { get; set; }
        public Nullable<int> ProcedureID { get; set; }
        public Nullable<System.DateTime> AppointmentDateTime { get; set; }
        public string Status { get; set; }
    
        public virtual Doctors Doctors { get; set; }
        public virtual Patients Patients { get; set; }
        public virtual Procedures Procedures { get; set; }
    }
}
