using System;

namespace InputMethodFix;

public interface IImeService
{
	string CompositionString { get; }

	bool IsCandidateListVisible { get; }

	uint SelectedCandidate { get; }

	uint CandidateCount { get; }

	bool IsEnabled { get; }

	string GetCandidate(uint index);

	void Enable();

	void Disable();

	void AddKeyListener(Action<char> listener);

	void RemoveKeyListener(Action<char> listener);
}
