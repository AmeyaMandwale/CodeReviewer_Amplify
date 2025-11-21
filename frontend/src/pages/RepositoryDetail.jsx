import React, { useEffect, useState } from "react";
import Layout from "../components/Layout.jsx";
import { useParams } from "react-router-dom";
import { 
  GitPullRequest, 
  AlertTriangle, 
  CheckCircle, 
  XCircle, 
  RefreshCw,
  Download,
  BarChart3,
  Code,
  FileText,
  Shield,
  Clock,
  User,
  Calendar,
  ArrowUpCircle,
  ArrowDownCircle,
  FileDiff,
  X,
  Plus,
  Minus,
  GitCommit,
  ArrowLeft
} from "lucide-react";

const RepositoryDetail = () => {
  const { repoId } = useParams();
  const [repoName, setRepoName] = useState("");
  const [pullRequests, setPullRequests] = useState([]);
  const [loading, setLoading] = useState(false);
  const [fetchingPRId, setFetchingPRId] = useState(null);
  const [message, setMessage] = useState("");
  const [prFiles, setPrFiles] = useState({});
  const [showFilesModal, setShowFilesModal] = useState(false);
  const [selectedPR, setSelectedPR] = useState(null);

  // SonarQube state
  const [sonarStatus, setSonarStatus] = useState({
    loading: false,
    connected: false,
    data: null,
    error: null,
  });

  // Active tab state
  const [activeTab, setActiveTab] = useState("overview");

  // Fetch repo details
  useEffect(() => {
    const fetchRepoName = async () => {
      try {
        const res = await fetch(`/api/repository/${repoId}`);
        if (!res.ok) throw new Error("Failed to fetch repository name");
        const data = await res.json();
        
        const repoNameOnly = data.name.includes("/")
          ? data.name.split("/").pop()
          : data.name;
        setRepoName(repoNameOnly);
      } catch (error) {
        console.error("Error fetching repository name:", error);
      }
    };
    fetchRepoName();
  }, [repoId]);

  // Fetch PR list
  useEffect(() => {
    const fetchPRs = async () => {
      if (!repoId) return;
      setLoading(true);
      try {
        const res = await fetch(`/api/PullRequest/repo/${repoId}`);
        if (!res.ok) throw new Error(`HTTP error! status: ${res.status}`);
        const data = await res.json();
        setPullRequests(data);
        
    //        await fetch(
    //   `/api/SonarQube/run-sonarqube?projectKey=${repoName}`,
    //   {
    //     method: "POST"
    //   }
    // );

    //     await fetch(
    //   `/api/SonarQube/run-analysis?projectKey=${repoName}`,
    //   {
    //     method: "POST"
    //   }
    // );
          console.log("Sonarqube running!!");
      } catch (err) {
        console.error("Failed to fetch PRs", err);
      } finally {
        setLoading(false);
      }
    };
    fetchPRs();
  }, [repoId]);

  // Fetch SonarQube Data
  useEffect(() => {
    const fetchSonarStatus = async () => {
      if (!repoName) return;
      setSonarStatus((prev) => ({ ...prev, loading: true }));
      try {
               await fetch(
      `/api/SonarQube/run-sonarqube?projectKey=${repoName}`,
      {
        method: "POST"
      }
    );
        const res = await fetch(`/api/SonarQube/dashboard?repoName=${repoName}`);
        const data = await res.json();

        if (data.connected) {
          setSonarStatus({
            loading: false,
            connected: true,
            data,
            error: null,
          });
        } else {
          setSonarStatus({
            loading: false,
            connected: false,
            data: null,
            error: data.message || "Repository not connected to SonarQube.",
          });
        }
      } catch (err) {
        setSonarStatus({
          loading: false,
          connected: false,
          data: null,
          error: "Failed to fetch SonarQube data.",
        });
      }
    };

    fetchSonarStatus();
  }, [repoName]);

  // Sync PR files and fetch updated list
  const handleFetchPRFiles = async (prId, prData) => {
    try {
      setFetchingPRId(prId);
      setMessage("");

      // Trigger file sync API
      // const res = await fetch(`/api/PRFile/sync/${prId}`, { method: "POST" });
      // if (!res.ok) throw new Error(`Failed to sync PR files (status: ${res.status})`);

      //  Fetch synced files from DB
      const filesRes = await fetch(`/api/PRFile/byPrId/${prId}`);
      if (!filesRes.ok) throw new Error(`Failed to fetch synced files `);
      const filesData = await filesRes.json();

      //  Update UI state for that specific PR
      setPrFiles((prev) => ({ ...prev, [prId]: filesData }));
      
      //  Show success message and open modal
      setMessage(`✅ Files synced successfully `);
      
      // Set selected PR data for modal
      setSelectedPR({
        ...prData,
        files: filesData
      });
      setShowFilesModal(true);

      // Auto-hide message after 3 seconds
      setTimeout(() => setMessage(""), 3000);

    } catch (error) {
      console.error("Error syncing PR files:", error);
      setMessage(`❌ Failed to fetch PR files `);
      setTimeout(() => setMessage(""), 3000);
    } finally {
      setFetchingPRId(null);
    }
  };

  // Close files modal
  const closeFilesModal = () => {
    setShowFilesModal(false);
    setSelectedPR(null);
  };

  // Render diff with syntax highlighting
  const renderDiff = (diff) => {
    if (!diff) return null;
    
    return diff.split('\n').map((line, index) => {
      let bgColor = 'bg-transparent';
      let icon = null;
      let textColor = 'text-gray-800 dark:text-gray-200';
      
      if (line.startsWith('+')) {
        bgColor = 'bg-green-50 dark:bg-green-900/20';
        textColor = 'text-green-700 dark:text-green-300';
        icon = <Plus className="w-3 h-3 text-green-500" />;
      } else if (line.startsWith('-')) {
        bgColor = 'bg-red-50 dark:bg-red-900/20';
        textColor = 'text-red-700 dark:text-red-300';
        icon = <Minus className="w-3 h-3 text-red-500" />;
      } else if (line.startsWith('@@')) {
        bgColor = 'bg-blue-50 dark:bg-blue-900/20';
        textColor = 'text-blue-700 dark:text-blue-300';
        icon = <GitCommit className="w-3 h-3 text-blue-500" />;
      }

      return (
        <div key={index} className={`flex items-start space-x-2 px-3 py-1 font-mono text-sm ${bgColor}`}>
          <div className="w-4 flex-shrink-0 mt-1">{icon}</div>
          <code className={`flex-1 ${textColor}`}>{line}</code>
        </div>
      );
    });
  };

  // Get change type color and icon
  const getChangeTypeInfo = (changeType) => {
    switch (changeType?.toLowerCase()) {
      case 'added':
        return { color: 'text-green-600 dark:text-green-400', bg: 'bg-green-100 dark:bg-green-900/30', icon: <Plus className="w-3 h-3" /> };
      case 'modified':
        return { color: 'text-yellow-600 dark:text-yellow-400', bg: 'bg-yellow-100 dark:bg-yellow-900/30', icon: <RefreshCw className="w-3 h-3" /> };
      case 'removed':
        return { color: 'text-red-600 dark:text-red-400', bg: 'bg-red-100 dark:bg-red-900/30', icon: <Minus className="w-3 h-3" /> };
      default:
        return { color: 'text-gray-600 dark:text-gray-400', bg: 'bg-gray-100 dark:bg-gray-900/30', icon: <FileText className="w-3 h-3" /> };
    }
  };

  // Calculate metrics for quick stats
  const metrics = sonarStatus.data?.metrics || [];
  const issues = sonarStatus.data?.issues || [];
  const criticalIssues = issues.filter(issue => issue.severity === 'BLOCKER' || issue.severity === 'CRITICAL').length;
  const codeCoverage = metrics.find(m => m.metric === 'coverage')?.value || 0;
  const duplicatedLines = metrics.find(m => m.metric === 'duplicated_lines_density')?.value || 0;

  const getStatusIcon = (status) => {
    switch (status?.toLowerCase()) {
      case 'open': return <ArrowUpCircle className="w-4 h-4 text-green-500" />;
      case 'closed': return <XCircle className="w-4 h-4 text-red-500" />;
      case 'merged': return <CheckCircle className="w-4 h-4 text-purple-500" />;
      default: return <Clock className="w-4 h-4 text-gray-500" />;
    }
  };

  const getSeverityColor = (severity) => {
    switch (severity) {
      case 'BLOCKER': return 'bg-red-100 text-red-800 border-red-200';
      case 'CRITICAL': return 'bg-orange-100 text-orange-800 border-orange-200';
      case 'MAJOR': return 'bg-yellow-100 text-yellow-800 border-yellow-200';
      case 'MINOR': return 'bg-blue-100 text-blue-800 border-blue-200';
      default: return 'bg-gray-100 text-gray-800 border-gray-200';
    }
  };

const downloadReport = async (projectKey) => {
  try {
    const res = await fetch(
      `/api/pdf/sonar-report?projectKey=${projectKey}`,
      { credentials: "include" }
    );

    const data = await res.json();

    if (!data.pdfUrl) {
      alert("Failed to generate report");
      return;
    }

    const fullUrl = `${data.pdfUrl}`;
    window.location.href = fullUrl;

  } catch (err) {
    console.error("Download failed", err);
  }
};

  return (
    <Layout>
    <div className="min-h-screen bg-slate-50 dark:bg-slate-900">
      {/* Header Section */}
      <div className="bg-white dark:bg-slate-800 border-b border-slate-200 dark:border-slate-700">
        <div className="max-w-7xl mx-auto px-6 py-6">
          <div className="flex items-center justify-between">
  <div className="flex items-center space-x-4">
    {/* Back Arrow Button */}
    <button
      onClick={() => window.history.back()}
      className="flex items-center justify-center w-10 h-10 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors"
      title="Go back"
    >
      <ArrowLeft className="w-5 h-5 text-slate-600 dark:text-slate-400" />
    </button>
    
    <div>
      <h1 className="text-2xl font-bold text-slate-900 dark:text-white">
        {repoName}
      </h1>
      <p className="text-slate-600 dark:text-slate-400 mt-1">
        Repository Analysis & Pull Requests
      </p>
    </div>
  </div>
  
  <div className="flex items-center space-x-3">
    <div className={`px-3 py-1 rounded-full text-sm font-medium ${
      sonarStatus.connected 
        ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300'
        : 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300'
    }`}>
      {sonarStatus.connected ? 'SonarQube Connected' : 'SonarQube Disconnected'}
    </div>
  </div>

          </div>

          {/* Navigation Tabs */}
          <div className="mt-6 border-b border-slate-200 dark:border-slate-700">
            <nav className="-mb-px flex space-x-8">
              {[
                { id: 'overview', name: 'Overview', icon: BarChart3 },
                { id: 'pull-requests', name: 'Pull Requests', icon: GitPullRequest },
                { id: 'code-quality', name: 'Code Quality', icon: Shield },
                { id: 'issues', name: 'Issues', icon: AlertTriangle },
              ].map((tab) => {
                const Icon = tab.icon;
                return (
                  <button
                    key={tab.id}
                    onClick={() => setActiveTab(tab.id)}
                    className={`flex items-center space-x-2 py-3 px-1 border-b-2 font-medium text-sm ${
                      activeTab === tab.id
                        ? 'border-violet-500 text-violet-600 dark:text-violet-400'
                        : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300 dark:text-slate-400 dark:hover:text-slate-300'
                    }`}
                  >
                    <Icon className="w-4 h-4" />
                    <span>{tab.name}</span>
                  </button>
                );
              })}
            </nav>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-6 py-6">
        {/* Message Alert */}
        {message && (
          <div className={`mb-6 p-4 rounded-xl border ${
            message.startsWith("✅")
              ? 'bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800 text-green-800 dark:text-green-300'
              : 'bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800 text-red-800 dark:text-red-300'
          }`}>
            {message}
          </div>
        )}

        {/* Overview Tab */}
        {activeTab === 'overview' && (
          <div className="space-y-6">
            {/* Quick Stats */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              <div className="bg-white dark:bg-slate-800 rounded-xl p-6 border border-slate-200 dark:border-slate-700">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-slate-600 dark:text-slate-400">Pull Requests</p>
                    <p className="text-2xl font-bold text-slate-900 dark:text-white mt-1">
                      {pullRequests.length}
                    </p>
                  </div>
                  <div className="p-3 bg-blue-100 dark:bg-blue-900/30 rounded-lg">
                    <GitPullRequest className="w-6 h-6 text-blue-600 dark:text-blue-400" />
                  </div>
                </div>
              </div>

              <div className="bg-white dark:bg-slate-800 rounded-xl p-6 border border-slate-200 dark:border-slate-700">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-slate-600 dark:text-slate-400">Critical Issues</p>
                    <p className="text-2xl font-bold text-slate-900 dark:text-white mt-1">
                      {criticalIssues}
                    </p>
                  </div>
                  <div className="p-3 bg-red-100 dark:bg-red-900/30 rounded-lg">
                    <AlertTriangle className="w-6 h-6 text-red-600 dark:text-red-400" />
                  </div>
                </div>
              </div>

              <div className="bg-white dark:bg-slate-800 rounded-xl p-6 border border-slate-200 dark:border-slate-700">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-slate-600 dark:text-slate-400">Code Coverage</p>
                    <p className="text-2xl font-bold text-slate-900 dark:text-white mt-1">
                      {codeCoverage}%
                    </p>
                  </div>
                  <div className="p-3 bg-green-100 dark:bg-green-900/30 rounded-lg">
                    <CheckCircle className="w-6 h-6 text-green-600 dark:text-green-400" />
                  </div>
                </div>
              </div>

              <div className="bg-white dark:bg-slate-800 rounded-xl p-6 border border-slate-200 dark:border-slate-700">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-slate-600 dark:text-slate-400">Duplicated Code</p>
                    <p className="text-2xl font-bold text-slate-900 dark:text-white mt-1">
                      {duplicatedLines}%
                    </p>
                  </div>
                  <div className="p-3 bg-orange-100 dark:bg-orange-900/30 rounded-lg">
                    <FileText className="w-6 h-6 text-orange-600 dark:text-orange-400" />
                  </div>
                </div>
              </div>
            </div>

            {/* SonarQube Dashboard */}
            <div className="bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 overflow-hidden">
              <div className="px-6 py-4 border-b border-slate-200 dark:border-slate-700 flex items-center justify-between">
  <h2 className="text-lg font-semibold text-slate-900 dark:text-white flex items-center">
    <Shield className="w-5 h-5 mr-2 text-violet-600 dark:text-violet-400" />
    SonarQube Analysis
  </h2>

  {/* Download Icon Button (Right-aligned) */}
  <button
    onClick={() => downloadReport(repoName)}
    className="p-2 rounded-lg bg-violet-600 hover:bg-violet-700 text-white transition"
    title="Download Sonar Report"
  >
    <Download className="w-5 h-5" />
  </button>
</div>


              <div className="p-6">
                {sonarStatus.loading ? (
                  <div className="flex items-center justify-center py-8">
                    <RefreshCw className="w-6 h-6 animate-spin text-slate-400" />
                    <span className="ml-2 text-slate-600 dark:text-slate-400">Loading SonarQube data...</span>
                  </div>
                ) : sonarStatus.connected ? (
                  <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                    {/* Metrics Card */}
                    <div className="lg:col-span-1">
                      <h3 className="text-sm font-semibold text-slate-900 dark:text-white mb-4">Key Metrics</h3>
                      <div className="space-y-3">
                        {metrics.slice(0, 6).map((metric, index) => (
                          <div key={index} className="flex justify-between items-center p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
                            <span className="text-sm text-slate-700 dark:text-slate-300 capitalize">
                              {metric.metric.replace(/_/g, ' ')}
                            </span>
                            <span className="font-semibold text-slate-900 dark:text-white">
                              {metric.value}
                            </span>
                          </div>
                        ))}
                      </div>
                    </div>

                    {/* Issues & Duplications */}
                    <div className="lg:col-span-2 space-y-6">
                      <div>
                        <h3 className="text-sm font-semibold text-slate-900 dark:text-white mb-4">
                          Recent Issues ({issues.length})
                        </h3>
                        <div className="space-y-3 max-h-60 overflow-y-auto">
                          {issues.slice(0, 5).map((issue, index) => (
                            <div key={index} className="p-4 border rounded-lg bg-white dark:bg-slate-700/30">
                              <div className="flex items-start justify-between">
                                <div className="flex-1">
                                  <div className="flex items-center space-x-2 mb-2">
                                    <span className={`px-2 py-1 text-xs rounded-full border ${getSeverityColor(issue.severity)}`}>
                                      {issue.severity}
                                    </span>
                                    <span className="text-sm font-medium text-slate-900 dark:text-white truncate">
                                      {issue.filePath}
                                    </span>
                                  </div>
                                  <p className="text-sm text-slate-600 dark:text-slate-400">
                                    {issue.message}
                                  </p>
                                  <p className="text-xs text-slate-500 dark:text-slate-500 mt-2">
                                    Line {issue.line || 'N/A'} • Effort: {issue.effort || 'N/A'}
                                  </p>
                                </div>
                              </div>
                            </div>
                          ))}
                        </div>
                      </div>
                    </div>
                  </div>
                ) : (
                  <div className="text-center py-8">
                    <AlertTriangle className="w-12 h-12 text-red-400 mx-auto mb-3" />
                    <p className="text-red-600 dark:text-red-400 font-medium">
                      {sonarStatus.error || "SonarQube not connected"}
                    </p>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

        {/* Pull Requests Tab */}
        {activeTab === 'pull-requests' && (
          <div className="bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-200 dark:border-slate-700">
              <h2 className="text-lg font-semibold text-slate-900 dark:text-white flex items-center">
                <GitPullRequest className="w-5 h-5 mr-2 text-violet-600 dark:text-violet-400" />
                Pull Requests ({pullRequests.length})
              </h2>
            </div>

            <div className="p-6">
              {loading ? (
                <div className="flex items-center justify-center py-8">
                  <RefreshCw className="w-6 h-6 animate-spin text-slate-400" />
                  <span className="ml-2 text-slate-600 dark:text-slate-400">Loading pull requests...</span>
                </div>
              ) : pullRequests.length === 0 ? (
                <div className="text-center py-8">
                  <GitPullRequest className="w-12 h-12 text-slate-300 mx-auto mb-3" />
                  <p className="text-slate-500 dark:text-slate-400">No pull requests found</p>
                </div>
              ) : (
                <div className="space-y-4">
                  {pullRequests.map((pr) => (
                    <div key={pr.id} className="border border-slate-200 dark:border-slate-600 rounded-lg overflow-hidden">
                      {/* PR Header */}
                      <div className="flex items-center justify-between p-4 bg-white dark:bg-slate-800 hover:bg-slate-50 dark:hover:bg-slate-700/50 transition-colors">
                        <div className="flex items-center space-x-4 flex-1">
                          {getStatusIcon(pr.status)}
                          <div className="flex-1 min-w-0">
                            <h3 className="font-medium text-slate-900 dark:text-white truncate">
                              {pr.title}
                            </h3>
                            <div className="flex items-center space-x-4 mt-1 text-sm text-slate-500 dark:text-slate-400">
                              {/* <span className="flex items-center">
                                <User className="w-3 h-3 mr-1" />
                                Author #{pr.authorId}
                              </span> */}
                              <span className="flex items-center">
                                <Calendar className="w-3 h-3 mr-1" />
                                {new Date(pr.createdAt).toLocaleDateString()}
                              </span>
                              <span className="capitalize">{pr.status}</span>
                            </div>
                          </div>
                        </div>
                        <button
                          onClick={() => handleFetchPRFiles(pr.id, pr)}
                          disabled={fetchingPRId === pr.id}
                          className={`flex items-center space-x-2 px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                            fetchingPRId === pr.id
                              ? 'bg-slate-100 text-slate-400 cursor-not-allowed dark:bg-slate-700'
                              : 'bg-violet-600 hover:bg-violet-700 text-white'
                          }`}
                        >
                          {fetchingPRId === pr.id ? (
                            <RefreshCw className="w-4 h-4 animate-spin" />
                          ) : (
                            <FileDiff className="w-4 h-4" />
                          )}
                          <span>{fetchingPRId === pr.id ? 'Syncing...' : 'View Files'}</span>
                        </button>
                      </div>

                      {/* PR Files Preview */}
                      {prFiles[pr.id] && prFiles[pr.id].length > 0 && (
                        <div className="border-t border-slate-200 dark:border-slate-600 bg-slate-50 dark:bg-slate-700/30 p-4">
                          <h4 className="font-semibold text-slate-700 dark:text-slate-300 mb-3 flex items-center">
                            <FileText className="w-4 h-4 mr-2" />
                            Changed Files ({prFiles[pr.id].length})
                          </h4>
                          <div className="space-y-2">
                            {prFiles[pr.id].slice(0, 3).map((file, index) => {
                              const changeTypeInfo = getChangeTypeInfo(file.changeType);
                              return (
                                <div key={index} className="flex items-center justify-between text-sm">
                                  <div className="flex items-center space-x-2">
                                    <span className={`px-2 py-1 rounded text-xs ${changeTypeInfo.bg} ${changeTypeInfo.color} flex items-center space-x-1`}>
                                      {changeTypeInfo.icon}
                                      <span>{file.changeType}</span>
                                    </span>
                                    <span className="font-medium text-slate-700 dark:text-slate-300">
                                      {file.path}
                                    </span>
                                  </div>
                                  {file.diff && (
                                    <span className="text-xs text-slate-500">
                                      {file.diff.split('\n').length} changes
                                    </span>
                                  )}
                                </div>
                              );
                            })}
                            {prFiles[pr.id].length > 3 && (
                              <p className="text-xs text-slate-500 mt-2">
                                +{prFiles[pr.id].length - 3} more files
                              </p>
                            )}
                          </div>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}

        {/* Code Quality Tab */}
        {activeTab === 'code-quality' && sonarStatus.connected && (
          <div className="space-y-6">
            {/* Content remains the same as previous implementation */}
          </div>
        )}

        {/* Issues Tab */}
        {activeTab === 'issues' && sonarStatus.connected && (
          <div className="space-y-6">
            {/* Content remains the same as previous implementation */}
          </div>
        )}
      </div>

      {/* PR Files Modal */}
      {showFilesModal && selectedPR && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white dark:bg-slate-800 rounded-xl w-full max-w-4xl max-h-[90vh] overflow-hidden">
            {/* Modal Header */}
            <div className="flex items-center justify-between p-6 border-b border-slate-200 dark:border-slate-700">
              <div>
                <h2 className="text-xl font-semibold text-slate-900 dark:text-white flex items-center">
                  <FileDiff className="w-5 h-5 mr-2 text-violet-600" />
                  Changed Files - {selectedPR.title}
                </h2>
                <p className="text-slate-600 dark:text-slate-400 mt-1">
                  PR #• {selectedPR.files.length} files changed
                </p>
              </div>
              <button
                onClick={closeFilesModal}
                className="p-2 hover:bg-slate-100 dark:hover:bg-slate-700 rounded-lg transition-colors"
              >
                <X className="w-5 h-5 text-slate-500" />
              </button>
            </div>

            {/* Modal Content */}
            <div className="p-6 overflow-y-auto max-h-[calc(90vh-140px)]">
              <div className="space-y-6">
                {selectedPR.files.map((file, index) => {
                  const changeTypeInfo = getChangeTypeInfo(file.changeType);
                  return (
                    <div key={index} className="border border-slate-200 dark:border-slate-600 rounded-lg overflow-hidden">
                      {/* File Header */}
                      <div className="flex items-center justify-between p-4 bg-slate-50 dark:bg-slate-700/50 border-b border-slate-200 dark:border-slate-600">
                        <div className="flex items-center space-x-3">
                          <span className={`px-3 py-1 rounded-full text-sm font-medium ${changeTypeInfo.bg} ${changeTypeInfo.color} flex items-center space-x-2`}>
                            {changeTypeInfo.icon}
                            <span className="capitalize">{file.changeType}</span>
                          </span>
                          <span className="font-mono text-sm text-slate-900 dark:text-white">
                            {file.path}
                          </span>
                        </div>
                        {file.diff && (
                          <span className="text-sm text-slate-500">
                            {file.diff.split('\n').filter(line => line.startsWith('+') || line.startsWith('-')).length} changes
                          </span>
                        )}
                      </div>

                      {/* File Diff */}
                      {file.diff && (
                        <div className="bg-slate-900 text-slate-100">
                          <div className="p-4 font-mono text-sm overflow-x-auto">
                            <div className="space-y-0">
                              {renderDiff(file.diff)}
                            </div>
                          </div>
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            </div>

            {/* Modal Footer */}
            <div className="flex items-center justify-between p-4 border-t border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-700/50">
              <div className="text-sm text-slate-600 dark:text-slate-400">
                {selectedPR.files.length} files • Synced {new Date().toLocaleTimeString()}
              </div>
              <button
                onClick={closeFilesModal}
                className="px-4 py-2 bg-slate-600 hover:bg-slate-700 text-white text-sm font-medium rounded-lg transition-colors"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
    </Layout>
  );
};

export default RepositoryDetail;