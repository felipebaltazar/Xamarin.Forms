﻿using System.ComponentModel;
using System.Threading.Tasks;

namespace Xamarin.Forms.Internals
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class PromptArguments
	{
		public PromptArguments(string title, string message, string accept = "OK", string cancel = "Cancel", string placeholder = null, int maxLength = -1, Keyboard keyboard = default(Keyboard))
		{
			Title = title;
			Message = message;
			Accept = accept;
			Cancel = cancel;
			Placeholder = placeholder;
			MaxLength = maxLength;
			Keyboard = keyboard ?? Keyboard.Default;
			Result = new TaskCompletionSource<string>();
		}

		public string Title { get; }

		public string Message { get; }

		public string Accept { get; }

		public string Cancel { get; }

		public string Placeholder { get; }

		public int MaxLength { get; }

		public Keyboard Keyboard { get; }

		public TaskCompletionSource<string> Result { get; }

		public void SetResult(string text)
		{
			Result.TrySetResult(text);
		}
	}
}