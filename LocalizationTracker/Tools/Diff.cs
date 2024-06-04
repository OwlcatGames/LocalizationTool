using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using DiffMatchPatch;
using JetBrains.Annotations;
using LocalizationTracker.Components;

namespace LocalizationTracker.Tools
{
	public static class Diff
	{
		[NotNull]
		public static InlinesWrapper MakeInlines(string before, string after)
		{
			if (before == null)
				before = "";
			if (after == null)
				after = "";

			List<DiffMatchPatch.Diff> diffs;
            try
            {
                diffs = DiffMatchPatchModule.Default.DiffMain(before, after);
                DiffMatchPatchModule.Default.DiffCleanupSemantic(diffs);
                DiffMatchPatchModule.Default.DiffCleanupEfficiency(diffs);
            }
            catch (Exception x)
            {
                throw new Exception($"Error when diffing strings:\n before <{before}>\nafter <{after}>", x);
            }

			var inlines = diffs
				.Select(MakeInline)
				.ToArray();

			return new InlinesWrapper(inlines);
		}

		[NotNull]
		private static InlineTemplate MakeInline(DiffMatchPatch.Diff diff)
		{
			var run = new InlineTemplate(diff.Text);
			if (Equals(diff.Operation, Operation.Delete))
			{
				run.Background = ColorUtility.Red;
				run.StrikeThrough = true;
				run.InlineType = InlineType.DiffDelete;
			}
			else if (Equals(diff.Operation, Operation.Insert))
			{
				run.Background = ColorUtility.Green;
                run.InlineType = InlineType.DiffInsert;
            }
            return run;
		}
	}
}