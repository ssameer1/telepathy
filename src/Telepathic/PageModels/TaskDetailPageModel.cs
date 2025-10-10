using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Telepathic.Models;
using Telepathic.Data.UserMemory;

namespace Telepathic.PageModels;

public partial class TaskDetailPageModel : ObservableObject, IQueryAttributable
{
	public const string ProjectQueryKey = "project";
	private ProjectTask? _task;
	private bool _canDelete; private readonly ProjectRepository _projectRepository;
	private readonly TaskRepository _taskRepository;
	private readonly ModalErrorHandler _errorHandler;
	private readonly TaskAssistAnalyzer? _taskAssistAnalyzer;
	private readonly TaskAssistHandler _taskAssistHandler;
	private readonly IUserMemoryStore _memoryStore;

	[ObservableProperty]
	private string _title = string.Empty;

	[ObservableProperty]
	private bool _isCompleted;

	[ObservableProperty]
	private List<Project> _projects = [];

	[ObservableProperty]
	private Project? _project;

	[ObservableProperty]
	private int _selectedProjectIndex = -1;

	[ObservableProperty]
	private bool _isExistingProject;

	[ObservableProperty]
	private AssistType _assistType = AssistType.None;

	[ObservableProperty]
	private string _assistData = string.Empty;

	[ObservableProperty]
	private bool _analyzeForAssist = true;
	public TaskDetailPageModel(
		ProjectRepository projectRepository,
		TaskRepository taskRepository,
		ModalErrorHandler errorHandler,
		TaskAssistHandler taskAssistHandler,
		IUserMemoryStore memoryStore,
		TaskAssistAnalyzer? taskAssistAnalyzer = null)
	{
		_projectRepository = projectRepository;
		_taskRepository = taskRepository;
		_errorHandler = errorHandler;
		_taskAssistAnalyzer = taskAssistAnalyzer;
		_taskAssistHandler = taskAssistHandler;
		_memoryStore = memoryStore;
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		LoadTaskAsync(query).FireAndForgetSafeAsync(_errorHandler);
	}

	private async Task LoadTaskAsync(IDictionary<string, object> query)
	{
		if (query.TryGetValue(ProjectQueryKey, out var project))
			Project = (Project)project;

		int taskId = 0;

		if (query.ContainsKey("id"))
		{
			taskId = Convert.ToInt32(query["id"]);
			_task = await _taskRepository.GetAsync(taskId);

			if (_task is null)
			{
				_errorHandler.HandleError(new Exception($"Task Id {taskId} isn't valid."));
				return;
			}

			Project = await _projectRepository.GetAsync(_task.ProjectID);
		}
		else
		{
			_task = new ProjectTask();
		}

		// If the project is new, we don't need to load the project dropdown
		if (Project?.ID == 0)
		{
			IsExistingProject = false;
		}
		else
		{
			Projects = await _projectRepository.ListAsync();
			IsExistingProject = true;
		}

		if (Project is not null)
			SelectedProjectIndex = Projects.FindIndex(p => p.ID == Project.ID);
		else if (_task?.ProjectID > 0)
			SelectedProjectIndex = Projects.FindIndex(p => p.ID == _task.ProjectID);

		if (taskId > 0)
		{
			if (_task is null)
			{
				_errorHandler.HandleError(new Exception($"Task with id {taskId} could not be found."));
				return;
			}

			Title = _task.Title;
			IsCompleted = _task.IsCompleted;
			AssistType = _task.AssistType;
			AssistData = _task.AssistData;
			CanDelete = true;
		}
		else
		{
			_task = new ProjectTask()
			{
				ProjectID = Project?.ID ?? 0
			};
		}
	}

	public bool CanDelete
	{
		get => _canDelete;
		set
		{
			_canDelete = value;
			DeleteCommand.NotifyCanExecuteChanged();
		}
	}


	partial void OnTitleChanged(string value)
	{
		// Optionally update _task.Title here if you want live sync
	}

	partial void OnAssistTypeChanged(AssistType value)
	{
		if (_task != null)
			_task.AssistType = value;
	}

	partial void OnAssistDataChanged(string value)
	{
		if (_task != null)
			_task.AssistData = value;
	}

	[RelayCommand]
	private void TitleUnfocused()
	{
		// Analyze for assist opportunities when title changes
		if (!string.IsNullOrWhiteSpace(Title) && AnalyzeForAssist && _taskAssistAnalyzer != null)
		{
			AnalyzeTaskTextAsync(Title).FireAndForgetSafeAsync(_errorHandler);
		}
	}

	private async Task AnalyzeTaskTextAsync(string text)
	{
		if (_taskAssistAnalyzer == null || string.IsNullOrWhiteSpace(text))
			return;

		try
		{
			// Create a temporary task object for analysis
			var tempTask = new ProjectTask { Title = text };

			// Analyze and set assist properties
			await _taskAssistAnalyzer.AnalyzeTaskAsync(tempTask);

			// Update the UI properties
			AssistType = tempTask.AssistType;
			AssistData = tempTask.AssistData;
		}
		catch (Exception ex)
		{
			_errorHandler.HandleError(ex);
		}
	}

	[RelayCommand]
	private async Task Save()
	{
		if (_task is null)
		{
			_errorHandler.HandleError(
				new Exception("Task or project is null. The task could not be saved."));

			return;
		}

		_task.Title = Title;

		int projectId = Project?.ID ?? 0;

		if (Projects.Count > SelectedProjectIndex && SelectedProjectIndex >= 0)
			_task.ProjectID = projectId = Projects[SelectedProjectIndex].ID;

		_task.IsCompleted = IsCompleted;

		// Save assist properties
		_task.AssistType = AssistType;
		_task.AssistData = AssistData;

		if (Project?.ID == projectId && !Project.Tasks.Contains(_task))
			Project.Tasks.Add(_task);

		var isNewTask = _task.ID == 0;

		if (_task.ProjectID > 0)
			_taskRepository.SaveItemAsync(_task).FireAndForgetSafeAsync(_errorHandler);

		// Track task creation in memory
		if (isNewTask)
		{
			await _memoryStore.LogEventAsync(MemoryEvent.Create(
				"task:create",
				_task.Title,
				new
				{
					projectName = Project?.Name,
					assistType = _task.AssistType.ToString(),
					source = "manual"
				},
				1.0));
		}

		await Shell.Current.GoToAsync("..?refresh=true");

		if (_task.ID > 0)
			await AppShell.DisplayToastAsync("Task saved");
	}

	[RelayCommand(CanExecute = nameof(CanDelete))]
	private async Task Delete()
	{
		if (_task is null || Project is null)
		{
			_errorHandler.HandleError(
				new Exception("Task is null. The task could not be deleted."));

			return;
		}

		if (Project.Tasks.Contains(_task))
			Project.Tasks.Remove(_task);

		if (_task.ID > 0)
			await _taskRepository.DeleteItemAsync(_task);

		await Shell.Current.GoToAsync("..?refresh=true");
		await AppShell.DisplayToastAsync("Task deleted");
	}
	[RelayCommand]
	async Task Assist()
	{
		if (_task == null || AssistType == AssistType.None)
			return;

		try
		{
			// Create a task object with current values to pass to the handler
			var taskToAssist = new ProjectTask
			{
				ID = _task.ID,
				Title = Title,
				AssistType = AssistType,
				AssistData = AssistData
			};

			// Use the shared TaskAssistHandler to process the assist action
			await _taskAssistHandler.HandleAssistAsync(taskToAssist, false);
		}
		catch (Exception ex)
		{
			_errorHandler.HandleError(ex);
		}
	}
}