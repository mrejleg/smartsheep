using Newtonsoft.Json;

namespace Project.Base.Models.DevExpress.DxGrids
{
    public class GroupResponseModel
    {
        /// <summary>
        /// The group's key.
        /// </summary>
        public object key { get; set; }

        /// <summary>
        /// Subgroups or data objects.
        /// </summary>
        public IList<GroupResponseModel> items { get; set; }

        /// <summary>
        /// The count of items in the group.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? count { get; set; }

        /// <summary>
        /// Group summary calculation results.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object[] summary { get; set; }
    }
}
