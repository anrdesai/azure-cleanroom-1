// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CgsUI.Models;

public class UpdatesViewModel
{
    public List<PendingProposal> Proposals { get; set; } = default!;

    public class PendingProposal
    {
        public string ProposalId { get; set; } = default!;

        public string ActionName { get; set; } = default!;
    }
}
