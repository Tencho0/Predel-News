'use strict';
(function () {
    var container = document.getElementById('poll-widget');
    if (!container) return;

    var pollData = JSON.parse(container.dataset.poll);
    if (!pollData) return;

    var cookieName = 'pn_voted_' + pollData.pollId;
    var hasVoted = document.cookie.split('; ').some(function (c) { return c.startsWith(cookieName + '='); });
    var tokenInput = container.closest('.card-body').querySelector('input[name="__RequestVerificationToken"]');
    var csrfToken = tokenInput ? tokenInput.value : '';

    if (pollData.isClosed || hasVoted) {
        renderResults(pollData.question, pollData.options, pollData.totalVotes, pollData.isClosed);
    } else {
        renderVotingForm(pollData);
    }

    function renderVotingForm(data) {
        var html = '<h5 class="card-title mb-3">' + escapeHtml(data.question) + '</h5>';
        html += '<div class="d-grid gap-2">';
        data.options.forEach(function (opt) {
            html += '<button type="button" class="btn btn-outline-primary text-start poll-vote-btn" data-option-id="' + opt.optionId + '">'
                  + escapeHtml(opt.text) + '</button>';
        });
        html += '</div>';
        container.innerHTML = html;

        container.querySelectorAll('.poll-vote-btn').forEach(function (btn) {
            btn.addEventListener('click', function () { submitVote(data.pollId, parseInt(btn.dataset.optionId)); });
        });
    }

    function renderResults(question, options, totalVotes, isClosed) {
        var html = '<h5 class="card-title mb-3">' + escapeHtml(question) + '</h5>';
        options.forEach(function (opt) {
            var pct = opt.percentage || 0;
            html += '<div class="mb-2">'
                  + '<div class="d-flex justify-content-between small mb-1">'
                  + '<span>' + escapeHtml(opt.text || opt.optionText || '') + '</span>'
                  + '<span>' + pct + '% (' + (opt.voteCount || 0) + ')</span>'
                  + '</div>'
                  + '<div class="progress" style="height: 20px;">'
                  + '<div class="progress-bar" style="width: 0%; transition: width 0.5s ease;" data-width="' + pct + '%"></div>'
                  + '</div></div>';
        });
        html += '<p class="text-muted small mt-2 mb-0">\u041e\u0431\u0449\u043e \u0433\u043b\u0430\u0441\u043e\u0432\u0435: ' + totalVotes + '</p>';
        if (isClosed) html += '<p class="text-muted small mb-0"><em>\u0410\u043d\u043a\u0435\u0442\u0430\u0442\u0430 \u0435 \u043f\u0440\u0438\u043a\u043b\u044e\u0447\u0438\u043b\u0430</em></p>';
        container.innerHTML = html;

        // Animate bars
        setTimeout(function () {
            container.querySelectorAll('.progress-bar').forEach(function (bar) {
                bar.style.width = bar.dataset.width;
            });
        }, 50);
    }

    async function submitVote(pollId, optionId) {
        container.querySelectorAll('.poll-vote-btn').forEach(function (b) { b.disabled = true; });

        try {
            var formData = new FormData();
            formData.append('pollId', pollId);
            formData.append('optionId', optionId);

            var response = await fetch('/api/poll/vote', {
                method: 'POST',
                headers: { 'X-CSRF-TOKEN': csrfToken },
                body: formData
            });

            var data = await response.json();

            if (data.status === 'success' || data.status === 'already_voted') {
                var totalVotes = data.results.reduce(function (sum, r) { return sum + r.voteCount; }, 0);
                renderResults(pollData.question, data.results, totalVotes, false);
            } else {
                container.querySelectorAll('.poll-vote-btn').forEach(function (b) { b.disabled = false; });
            }
        } catch (err) {
            container.querySelectorAll('.poll-vote-btn').forEach(function (b) { b.disabled = false; });
        }
    }

    function escapeHtml(str) {
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(str));
        return div.innerHTML;
    }
})();
