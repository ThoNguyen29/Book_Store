/**
 * SearchableSelect – Custom searchable dropdown with tag display
 *
 * Usage:
 *   new SearchableSelect({
 *     targetSelect: '#AuthorSelect',   // original <select> element
 *     multiple: true,                   // multi-select (tags) or single
 *     placeholder: 'Tìm tác giả...',
 *   });
 */
class SearchableSelect {
  constructor(options) {
    this.select =
      typeof options.targetSelect === "string"
        ? document.querySelector(options.targetSelect)
        : options.targetSelect;

    if (!this.select) return;

    this.multiple = options.multiple ?? this.select.multiple;
    this.placeholder = options.placeholder || "Tìm kiếm...";
    this.onAdd = options.onAdd || null; // callback when quick-add button clicked

    this.isOpen = false;
    this._build();
    this._bindEvents();
    this._syncFromSelect();
  }

  _build() {
    // Hide original select
    this.select.style.display = "none";

    // Create wrapper
    this.wrapper = document.createElement("div");
    this.wrapper.className = "ss-wrapper";

    // Display area (tags + input)
    this.display = document.createElement("div");
    this.display.className = "ss-display";

    this.tagsContainer = document.createElement("div");
    this.tagsContainer.className = "ss-tags";

    this.searchInput = document.createElement("input");
    this.searchInput.type = "text";
    this.searchInput.className = "ss-search-input";
    this.searchInput.placeholder = this.placeholder;
    this.searchInput.autocomplete = "off";

    this.display.appendChild(this.tagsContainer);
    this.display.appendChild(this.searchInput);

    // Arrow icon
    this.arrow = document.createElement("span");
    this.arrow.className = "ss-arrow";
    this.arrow.innerHTML = '<i class="fas fa-chevron-down"></i>';
    this.display.appendChild(this.arrow);

    // Dropdown
    this.dropdown = document.createElement("div");
    this.dropdown.className = "ss-dropdown";

    this.optionsList = document.createElement("div");
    this.optionsList.className = "ss-options";

    this.noResults = document.createElement("div");
    this.noResults.className = "ss-no-results";
    this.noResults.textContent = "Không tìm thấy kết quả";
    this.noResults.style.display = "none";

    this.dropdown.appendChild(this.optionsList);
    this.dropdown.appendChild(this.noResults);

    this.wrapper.appendChild(this.display);
    this.wrapper.appendChild(this.dropdown);

    // Insert after the original select
    this.select.parentNode.insertBefore(this.wrapper, this.select.nextSibling);
  }

  _bindEvents() {
    // Toggle dropdown on display click
    this.display.addEventListener("click", (e) => {
      if (e.target.closest(".ss-tag-remove")) return;
      this.searchInput.focus();
      this._toggle();
    });

    // Search input
    this.searchInput.addEventListener("input", () => {
      this._filterOptions();
      if (!this.isOpen) this._open();
    });

    this.searchInput.addEventListener("focus", () => {
      if (!this.isOpen) this._open();
    });

    // Keyboard navigation
    this.searchInput.addEventListener("keydown", (e) => {
      if (e.key === "Escape") {
        this._close();
      } else if (
        e.key === "Backspace" &&
        !this.searchInput.value &&
        this.multiple
      ) {
        // Remove last tag
        const tags = this.tagsContainer.querySelectorAll(".ss-tag");
        if (tags.length > 0) {
          const lastTag = tags[tags.length - 1];
          const val = lastTag.dataset.value;
          this._deselectValue(val);
        }
      }
    });

    // Close on outside click
    document.addEventListener("click", (e) => {
      if (!this.wrapper.contains(e.target)) {
        this._close();
      }
    });
  }

  _toggle() {
    if (this.isOpen) this._close();
    else this._open();
  }

  _open() {
    this.isOpen = true;
    this.wrapper.classList.add("ss-open");
    this._renderOptions();
    this._filterOptions();
  }

  _close() {
    this.isOpen = false;
    this.wrapper.classList.remove("ss-open");
    this.searchInput.value = "";
  }

  _renderOptions() {
    this.optionsList.innerHTML = "";
    const options = Array.from(this.select.options);

    options.forEach((opt) => {
      if (!opt.value) return; // skip placeholder options

      const item = document.createElement("div");
      item.className = "ss-option";
      item.dataset.value = opt.value;
      item.textContent = opt.textContent;

      if (opt.selected) {
        item.classList.add("ss-selected");
      }

      item.addEventListener("click", (e) => {
        e.stopPropagation();
        this._selectOption(opt, item);
      });

      this.optionsList.appendChild(item);
    });
  }

  _filterOptions() {
    const query = this.searchInput.value.toLowerCase().trim();
    const items = this.optionsList.querySelectorAll(".ss-option");
    let visibleCount = 0;

    items.forEach((item) => {
      const text = item.textContent.toLowerCase();
      const match = !query || text.includes(query);
      item.style.display = match ? "" : "none";
      if (match) visibleCount++;
    });

    this.noResults.style.display = visibleCount === 0 ? "block" : "none";
  }

  _selectOption(opt, item) {
    if (this.multiple) {
      opt.selected = !opt.selected;
      item.classList.toggle("ss-selected", opt.selected);
    } else {
      // Deselect all first
      Array.from(this.select.options).forEach((o) => (o.selected = false));
      this.optionsList
        .querySelectorAll(".ss-option")
        .forEach((i) => i.classList.remove("ss-selected"));

      opt.selected = true;
      item.classList.add("ss-selected");
      this._close();
    }

    this._syncTags();
    this._triggerChange();
  }

  _deselectValue(value) {
    const opt = Array.from(this.select.options).find((o) => o.value === value);
    if (opt) {
      opt.selected = false;
    }
    this._syncTags();
    this._renderOptions();
    this._triggerChange();
  }

  _syncTags() {
    this.tagsContainer.innerHTML = "";
    const selected = Array.from(this.select.options).filter(
      (o) => o.selected && o.value,
    );

    if (selected.length === 0) {
      this.searchInput.placeholder = this.placeholder;
      return;
    }

    this.searchInput.placeholder = "";

    selected.forEach((opt) => {
      const tag = document.createElement("span");
      tag.className = "ss-tag";
      tag.dataset.value = opt.value;

      const text = document.createElement("span");
      text.className = "ss-tag-text";
      text.textContent = opt.textContent;

      const remove = document.createElement("span");
      remove.className = "ss-tag-remove";
      remove.innerHTML = "&times;";
      remove.addEventListener("click", (e) => {
        e.stopPropagation();
        if (this.multiple) {
          this._deselectValue(opt.value);
        } else {
          this._deselectValue(opt.value);
        }
      });

      tag.appendChild(text);
      tag.appendChild(remove);
      this.tagsContainer.appendChild(tag);
    });
  }

  _syncFromSelect() {
    this._syncTags();
  }

  _triggerChange() {
    this.select.dispatchEvent(new Event("change", { bubbles: true }));
  }

  // Public method: add a new option and select it
  addOption(value, text, selected = true) {
    let opt = Array.from(this.select.options).find(
      (o) => o.value === String(value),
    );
    if (!opt) {
      opt = document.createElement("option");
      opt.value = value;
      opt.textContent = text;
      this.select.appendChild(opt);
    }

    if (selected) {
      if (!this.multiple) {
        Array.from(this.select.options).forEach((o) => (o.selected = false));
      }
      opt.selected = true;
    }

    this._syncTags();
    if (this.isOpen) this._renderOptions();
  }

  // Public method: refresh display from select state
  refresh() {
    this._syncTags();
    if (this.isOpen) this._renderOptions();
  }
}
