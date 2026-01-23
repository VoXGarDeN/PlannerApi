using System;

namespace PlannerApi.Models
{
    public class resource
    {
        public Guid uid { get; set; }
        public string name { get; set; } = "";
        public DateTime time_ins { get; set; }
        public Guid company_id { get; set; }
    }

    public class task
    {
        public Guid uid { get; set; }
        public string name { get; set; } = "";
        public DateTime time_ins { get; set; }
        public DateTime time_pref_start { get; set; }
        public DateTime time_pref_finish { get; set; }
        public int duration { get; set; }
        public Guid company_id { get; set; }
    }

    public class shift
    {
        public Guid uid { get; set; }
        public string name { get; set; } = "";
        public DateTime time_ins { get; set; }
        public Guid resource_id { get; set; }
        public DateTime time_start { get; set; }
        public DateTime time_finish { get; set; }
    }

    public class shift_task
    {
        public Guid shift_id { get; set; }
        public string shift_name { get; set; } = "";
        public Guid task_id { get; set; }
        public string task_name { get; set; } = "";
        public DateTime time_ins { get; set; }
        public DateTime time_sched_start { get; set; }
        public DateTime time_sched_finish { get; set; }
        public int? idle_dur { get; set; }
    }
}