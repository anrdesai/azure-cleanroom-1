// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CgsUI.Models;

public class ContractViewModel
{
    public string Id { get; set; } = default!;

    public string Version { get; set; } = default!;

    public string State { get; set; } = default!;

    public object Data { get; set; } = default!;

    public string ProposalId { get; set; } = default!;

    public List<FinalVotesViewModel> FinalVotes { get; set; } = default!;

    public class FinalVotesViewModel
    {
        public string MemberId { get; set; } = default!;

        public string MemberName { get; set; } = default!;

        public bool Vote { get; set; } = default!;
    }
}
