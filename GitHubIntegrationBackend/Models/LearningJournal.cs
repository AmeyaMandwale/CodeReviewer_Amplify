
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace GitHubIntegrationBackend.Models
{
    public class LearningJournal
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Organization")]
        public int OrgId { get; set; }

        [ForeignKey("Repository")]
        public int RepoId { get; set; }

        public string SourceType { get; set; } = string.Empty;
        public string PatternRecognized { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;

        public Organization? Organization { get; set; }
        public Repository? Repository { get; set; }
    }
}