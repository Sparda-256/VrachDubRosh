﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class PomoshnikPolicliniki2Entities1 : DbContext
    {
        public PomoshnikPolicliniki2Entities1()
            : base("name=PomoshnikPolicliniki2Entities1")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Answers> Answers { get; set; }
        public virtual DbSet<ChiefDoctors> ChiefDoctors { get; set; }
        public virtual DbSet<Diagnoses> Diagnoses { get; set; }
        public virtual DbSet<Doctors> Doctors { get; set; }
        public virtual DbSet<FollowUpQuestions> FollowUpQuestions { get; set; }
        public virtual DbSet<NewPatients> NewPatients { get; set; }
        public virtual DbSet<Patients> Patients { get; set; }
        public virtual DbSet<ProcedureAppointments> ProcedureAppointments { get; set; }
        public virtual DbSet<Procedures> Procedures { get; set; }
        public virtual DbSet<Symptoms> Symptoms { get; set; }
    }
}
