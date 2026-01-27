/**
 * Predel News - Site JavaScript
 * Mobile-first, progressive enhancement
 */

(function() {
    'use strict';

    // DOM Ready
    document.addEventListener('DOMContentLoaded', init);

    function init() {
        initMobileMenu();
        initSearchModal();
        initStickyHeader();
        initLazyImages();
        initShareButtons();
    }

    /**
     * Mobile Menu Toggle
     */
    function initMobileMenu() {
        const menuToggle = document.querySelector('.menu-toggle');
        const mobileMenu = document.getElementById('mobile-menu');
        const menuClose = document.querySelector('.mobile-menu-close');
        const menuOverlay = document.querySelector('.mobile-menu-overlay');

        if (!menuToggle || !mobileMenu) return;

        function openMenu() {
            mobileMenu.hidden = false;
            menuToggle.setAttribute('aria-expanded', 'true');
            document.body.style.overflow = 'hidden';
            setTimeout(() => {
                mobileMenu.querySelector('.mobile-menu-close')?.focus();
            }, 100);
        }

        function closeMenu() {
            mobileMenu.hidden = true;
            menuToggle.setAttribute('aria-expanded', 'false');
            document.body.style.overflow = '';
            menuToggle.focus();
        }

        menuToggle.addEventListener('click', openMenu);
        menuClose?.addEventListener('click', closeMenu);
        menuOverlay?.addEventListener('click', closeMenu);

        // Close on Escape
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape' && !mobileMenu.hidden) {
                closeMenu();
            }
        });

        // Close on window resize (tablet+)
        window.addEventListener('resize', function() {
            if (window.innerWidth >= 768 && !mobileMenu.hidden) {
                closeMenu();
            }
        });
    }

    /**
     * Search Modal
     */
    function initSearchModal() {
        const searchToggle = document.querySelector('.search-toggle');
        const searchModal = document.getElementById('search-modal');
        const searchClose = document.querySelector('.search-close');
        const searchOverlay = document.querySelector('.search-modal-overlay');
        const searchInput = document.getElementById('search-input');

        if (!searchToggle || !searchModal) return;

        function openSearch() {
            searchModal.hidden = false;
            searchToggle.setAttribute('aria-expanded', 'true');
            document.body.style.overflow = 'hidden';
            setTimeout(() => {
                searchInput?.focus();
            }, 100);
        }

        function closeSearch() {
            searchModal.hidden = true;
            searchToggle.setAttribute('aria-expanded', 'false');
            document.body.style.overflow = '';
            searchToggle.focus();
        }

        searchToggle.addEventListener('click', openSearch);
        searchClose?.addEventListener('click', closeSearch);
        searchOverlay?.addEventListener('click', closeSearch);

        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape' && !searchModal.hidden) {
                closeSearch();
            }
            // Ctrl/Cmd + K to open search
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                if (searchModal.hidden) {
                    openSearch();
                } else {
                    closeSearch();
                }
            }
        });
    }

    /**
     * Sticky Header with hide on scroll down
     */
    function initStickyHeader() {
        const header = document.querySelector('.site-header');
        if (!header) return;

        let lastScrollY = window.scrollY;
        let ticking = false;

        function updateHeader() {
            const currentScrollY = window.scrollY;

            if (currentScrollY > 100) {
                header.classList.add('is-scrolled');
            } else {
                header.classList.remove('is-scrolled');
            }

            // Optional: Hide header on scroll down, show on scroll up
            // Uncomment below if desired
            /*
            if (currentScrollY > lastScrollY && currentScrollY > 200) {
                header.classList.add('is-hidden');
            } else {
                header.classList.remove('is-hidden');
            }
            */

            lastScrollY = currentScrollY;
            ticking = false;
        }

        window.addEventListener('scroll', function() {
            if (!ticking) {
                window.requestAnimationFrame(updateHeader);
                ticking = true;
            }
        }, { passive: true });
    }

    /**
     * Lazy Loading Images with Intersection Observer
     */
    function initLazyImages() {
        // Native lazy loading is supported, but this adds fade-in effect
        const images = document.querySelectorAll('img[loading="lazy"]');

        if ('IntersectionObserver' in window) {
            const imageObserver = new IntersectionObserver(function(entries) {
                entries.forEach(function(entry) {
                    if (entry.isIntersecting) {
                        const img = entry.target;
                        img.classList.add('is-loaded');
                        imageObserver.unobserve(img);
                    }
                });
            }, {
                rootMargin: '50px 0px'
            });

            images.forEach(function(img) {
                imageObserver.observe(img);
            });
        } else {
            // Fallback for older browsers
            images.forEach(function(img) {
                img.classList.add('is-loaded');
            });
        }
    }

    /**
     * Share Buttons - Copy URL fallback
     */
    function initShareButtons() {
        const shareLinks = document.querySelectorAll('.share-link');

        shareLinks.forEach(function(link) {
            link.addEventListener('click', function(e) {
                // Let the link work normally for social shares
                // This just adds analytics or additional handling if needed
            });
        });

        // Copy URL button if present
        const copyButton = document.querySelector('.share-copy');
        if (copyButton) {
            copyButton.addEventListener('click', function(e) {
                e.preventDefault();
                const url = window.location.href;

                if (navigator.clipboard) {
                    navigator.clipboard.writeText(url).then(function() {
                        showToast('URL copied to clipboard');
                    }).catch(function() {
                        fallbackCopy(url);
                    });
                } else {
                    fallbackCopy(url);
                }
            });
        }
    }

    function fallbackCopy(text) {
        const textarea = document.createElement('textarea');
        textarea.value = text;
        textarea.style.position = 'fixed';
        textarea.style.opacity = '0';
        document.body.appendChild(textarea);
        textarea.select();
        try {
            document.execCommand('copy');
            showToast('URL copied to clipboard');
        } catch (err) {
            showToast('Failed to copy URL');
        }
        document.body.removeChild(textarea);
    }

    function showToast(message) {
        const existing = document.querySelector('.toast');
        if (existing) {
            existing.remove();
        }

        const toast = document.createElement('div');
        toast.className = 'toast';
        toast.textContent = message;
        toast.setAttribute('role', 'status');
        toast.setAttribute('aria-live', 'polite');

        document.body.appendChild(toast);

        setTimeout(function() {
            toast.classList.add('is-visible');
        }, 10);

        setTimeout(function() {
            toast.classList.remove('is-visible');
            setTimeout(function() {
                toast.remove();
            }, 300);
        }, 2000);
    }

    // Expose utility functions globally if needed
    window.PredelNews = {
        showToast: showToast
    };

})();
