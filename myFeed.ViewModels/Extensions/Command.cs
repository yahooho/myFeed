﻿using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace myFeed.ViewModels.Extensions
{
    /// <summary>
    /// Command that dispatches task-actions.
    /// </summary>
    public class Command : ICommand
    {
        private readonly Func<Task> _task;
        private bool _canExecute = true;

        /// <summary>
        /// Creates new instance using task lambda expression as a command.
        /// </summary>
        /// <param name="task">Task to execute.</param>
        public Command(Func<Task> task) => _task = task;

        /// <summary>
        /// Creates new instance using action lambda expression as a command.
        /// </summary>
        /// <param name="action">Task to execute.</param>
        public Command(Action action) => _task = async () => action.Invoke();

        /// <summary>
        /// True if queue is unlocked and command can be executed.
        /// </summary>
        public bool CanExecute(object parameter) => _canExecute;

        /// <summary>
        /// Invoked when command state changes.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Execute a command.
        /// </summary>
        public async void Execute(object parameter)
        {
            UpdateCanExecute(false);
            await _task.Invoke();
            UpdateCanExecute(true);

            void UpdateCanExecute(bool value)
            {
                _canExecute = value;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}