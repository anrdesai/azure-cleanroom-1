// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CgsUI.Models;

public class ProposalViewModel
{
    public string ProposalId { get; set; } = default!;

    public bool IsOpen { get; set; } = default!;

    public bool CanWithdraw { get; set; }

    public bool CanVote { get; set; }

    public string? DisableVoteReason { get; set; }

    public string Proposal { get; set; } = default!;

    public List<VotesViewModel> Votes { get; set; } = default!;

    public string Actions { get; set; } = default!;

    public class VotesViewModel
    {
        public string MemberId { get; set; } = default!;

        public string MemberName { get; set; } = default!;

        public string Vote { get; set; } = default!;
    }
}
