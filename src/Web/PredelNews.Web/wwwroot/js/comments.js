(function () {
  "use strict";

  var form = document.getElementById("comment-form");
  if (!form) return;

  var commentList = document.getElementById("comment-list");
  var messageBox = document.getElementById("comment-message");
  var submitBtn = document.getElementById("comment-submit");

  form.addEventListener("submit", function (e) {
    e.preventDefault();
    clearErrors();

    var formData = new FormData(form);
    submitBtn.disabled = true;
    submitBtn.textContent = "Изпращане...";

    fetch("/api/comments", {
      method: "POST",
      body: formData,
      credentials: "same-origin",
      headers: {
        "X-Requested-With": "XMLHttpRequest"
      }
    })
      .then(function (response) {
        return response.json().then(function (data) {
          return { status: response.status, data: data };
        });
      })
      .then(function (result) {
        var data = result.data;

        if (result.status === 429) {
          showMessage(data.message, "warning");
          return;
        }

        if (result.status === 400 && data.errors) {
          showValidationErrors(data.errors);
          return;
        }

        if (data.status === "accepted" && data.comment) {
          appendComment(data.comment);
          form.reset();
          // Preserve the display name in the field
          var nameInput = document.getElementById("displayName");
          if (data.comment.displayName) {
            nameInput.value = data.comment.displayName;
          }
          showMessage("Коментарът е публикуван успешно.", "success");
          updateCommentCount(1);
          return;
        }

        if (data.status === "held") {
          form.reset();
          showMessage(data.message, "info");
          return;
        }

        // Honeypot or silent discard — fake success
        if (data.status === "accepted") {
          form.reset();
          showMessage("Коментарът е публикуван.", "success");
          return;
        }
      })
      .catch(function () {
        showMessage("Възникна грешка. Моля, опитайте отново.", "danger");
      })
      .finally(function () {
        submitBtn.disabled = false;
        submitBtn.textContent = "Публикувай";
      });
  });

  // Delete comment handler (delegated)
  if (commentList) {
    commentList.addEventListener("click", function (e) {
      var btn = e.target.closest(".pn-comment-delete");
      if (!btn) return;

      var commentId = btn.getAttribute("data-comment-id");
      if (!confirm("Сигурни ли сте, че искате да изтриете този коментар?")) return;

      btn.disabled = true;

      var csrfToken = document.querySelector('input[name="__RequestVerificationToken"]');
      var headers = { "X-Requested-With": "XMLHttpRequest" };
      if (csrfToken) {
        headers["X-CSRF-TOKEN"] = csrfToken.value;
      }

      fetch("/api/comments/" + commentId, {
        method: "DELETE",
        headers: headers
      })
        .then(function (response) {
          if (response.ok) {
            var commentEl = btn.closest(".pn-comment");
            if (commentEl) commentEl.remove();
            updateCommentCount(-1);
          } else {
            alert("Грешка при изтриване на коментара.");
            btn.disabled = false;
          }
        })
        .catch(function () {
          alert("Грешка при изтриване на коментара.");
          btn.disabled = false;
        });
    });
  }

  function appendComment(comment) {
    var noMsg = document.getElementById("no-comments-message");
    if (noMsg) noMsg.remove();

    var div = document.createElement("div");
    div.className = "pn-comment border-bottom py-3";
    div.setAttribute("data-comment-id", comment.id);
    div.innerHTML =
      '<div>' +
      '<strong class="d-block mb-1">' + escapeHtml(comment.displayName) + '</strong>' +
      '<small class="text-meta">' + escapeHtml(comment.createdAt) + '</small>' +
      '</div>' +
      '<p class="mt-2 mb-0">' + escapeHtml(comment.commentText) + '</p>';

    commentList.appendChild(div);
  }

  function showMessage(text, type) {
    messageBox.textContent = text;
    messageBox.className = "alert alert-" + type;
    messageBox.classList.remove("d-none");
    setTimeout(function () {
      messageBox.classList.add("d-none");
    }, 6000);
  }

  function showValidationErrors(errors) {
    for (var field in errors) {
      var errorEl = document.getElementById(field + "-error");
      var inputEl = document.getElementById(field);
      if (errorEl && inputEl) {
        errorEl.textContent = errors[field];
        inputEl.classList.add("is-invalid");
      }
    }
  }

  function clearErrors() {
    var invalids = form.querySelectorAll(".is-invalid");
    for (var i = 0; i < invalids.length; i++) {
      invalids[i].classList.remove("is-invalid");
    }
    messageBox.classList.add("d-none");
  }

  function updateCommentCount(delta) {
    var countEl = document.getElementById("comment-count");
    if (countEl) {
      var current = parseInt(countEl.textContent, 10) || 0;
      countEl.textContent = current + delta;
    }
  }

  function escapeHtml(text) {
    var div = document.createElement("div");
    div.appendChild(document.createTextNode(text));
    return div.innerHTML;
  }
})();
