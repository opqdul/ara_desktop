(function () {
    if (window.__araDesktopBridgeInstalled) {
        return;
    }
    window.__araDesktopBridgeInstalled = true;

    function hostPost(handler, args) {
        try {
            if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
                window.chrome.webview.postMessage(JSON.stringify({ handler: handler, args: args || [] }));
            }
        } catch (_) { }
    }

    if (!window.flutter_inappwebview) {
        window.flutter_inappwebview = {
            callHandler: function (name) {
                var args = Array.prototype.slice.call(arguments, 1);
                hostPost(name, args);
            }
        };
    }

    window.setAraAssessmentMode = function (mode) {
        hostPost('onAssessmentStateChange', [mode]);
    };

    function resolvePortalBaseUrl() {
        try {
            var origin = window.location.origin || '';
            var path = window.location.pathname || '';
            if (path.indexOf('/new_ara') === 0) {
                return origin + '/new_ara';
            }
            if (path.indexOf('/assess') === 0) {
                return origin + '/assess';
            }
            var parts = path.split('/').filter(Boolean);
            var ignored = ['login', 'home', 'exam', 'user', 'violation', 'dashboard', 'result'];
            if (parts.length > 0 && ignored.indexOf(parts[0].toLowerCase()) === -1) {
                return origin + '/' + parts[0];
            }
            return origin;
        } catch (_) {
            return '';
        }
    }

    var lastContextSignature = '';

    function syncExamContextFromDOM() {
        try {
            var userEl = document.getElementById('hid_user_id');
            var examEl = document.getElementById('hid_exam_id');
            var classEl = document.getElementById('hid_class_id');
            var respEl = document.getElementById('hid_response');

            var userID = (userEl && userEl.value) ? String(userEl.value).trim() : (localStorage.getItem('ara_last_user_id') || '');
            var examID = (examEl && examEl.value) ? String(examEl.value).trim() : (localStorage.getItem('ara_last_exam_id') || '');
            var classID = (classEl && classEl.value) ? String(classEl.value).trim() : (localStorage.getItem('ara_last_class_id') || '');
            var responseID = (respEl && respEl.value) ? String(respEl.value).trim() : (localStorage.getItem('ara_last_response_id') || '');
            var baseUrl = resolvePortalBaseUrl();

            if (userEl && userEl.value) { localStorage.setItem('ara_last_user_id', userID); }
            if (examEl && examEl.value) { localStorage.setItem('ara_last_exam_id', examID); }
            if (classEl && classEl.value) { localStorage.setItem('ara_last_class_id', classID); }
            if (respEl && respEl.value) { localStorage.setItem('ara_last_response_id', responseID); }
            if (baseUrl) { localStorage.setItem('ara_last_base_url', baseUrl); }

            var ctx = {
                baseUrl: baseUrl || localStorage.getItem('ara_last_base_url') || '',
                userID: userID,
                examID: examID,
                classID: classID,
                responseID: responseID
            };

            var signature = JSON.stringify(ctx);
            if (signature !== lastContextSignature) {
                lastContextSignature = signature;
                hostPost('onExamContextUpdate', [ctx]);
            }
            return ctx;
        } catch (_) {
            return {};
        }
    }

    window.recordAraDesktopViolation = function (violationType, violationLabel, description, source) {
        try {
            if (typeof window.recordAraViolation === 'function') {
                window.recordAraViolation(
                    violationType || 'app_exit',
                    violationLabel || 'Keluar dari Aplikasi',
                    description || '',
                    source || 'ara_desktop',
                    5000
                );
                return true;
            }

            hostPost('onViolation', [
                violationType || 'app_exit',
                violationLabel || 'Keluar dari Aplikasi',
                description || '',
                source || 'ara_desktop'
            ]);
            return true;
        } catch (_) {
            return false;
        }
    };

    try {
        history.pushState(null, document.title, location.href);
        window.addEventListener('popstate', function () {
            history.pushState(null, document.title, location.href);
        });
    } catch (_) { }

    document.addEventListener('keydown', function (e) {
        if ((e.key === 'F12' || e.keyCode === 123) && (e.ctrlKey || e.altKey || e.shiftKey)) {
            hostPost('onSupervisorPanelTrigger', []);
        }

        if (e.altKey && (e.key === 'ArrowLeft' || e.key === 'ArrowRight' || e.keyCode === 37 || e.keyCode === 39)) {
            e.preventDefault();
            e.stopPropagation();
            return false;
        }

        if (e.key === 'Backspace' || e.keyCode === 8) {
            var el = document.activeElement;
            var tag = el ? el.tagName.toUpperCase() : '';
            var editable = (tag === 'INPUT' || tag === 'TEXTAREA' || (el && el.isContentEditable));
            if (!editable) {
                e.preventDefault();
                e.stopPropagation();
                return false;
            }
        }
    });

    var lastReportedMode = '';

    function reportMode(mode) {
        if (mode === lastReportedMode) {
            return;
        }
        lastReportedMode = mode;
        hostPost('onAssessmentStateChange', [mode]);
    }

    function detectStateFromDOM() {
        syncExamContextFromDOM();

        var pathname = (window.location.pathname || '').toLowerCase();
        var isNormal = pathname === '' ||
            pathname === '/' ||
            pathname === '/assess' ||
            pathname === '/assess/' ||
            pathname.indexOf('/login') !== -1 ||
            pathname.indexOf('/register') !== -1 ||
            pathname.indexOf('/dashboard') !== -1 ||
            pathname.indexOf('/profile') !== -1 ||
            pathname.indexOf('/user') !== -1 ||
            pathname.indexOf('/biodata') !== -1 ||
            pathname.indexOf('/verifikasi') !== -1 ||
            pathname.indexOf('/verification') !== -1 ||
            pathname.indexOf('/step') !== -1 ||
            pathname.indexOf('/instruksi') !== -1 ||
            pathname.indexOf('/instruction') !== -1 ||
            pathname.indexOf('/jadwal') !== -1 ||
            pathname.indexOf('/schedule') !== -1 ||
            pathname.indexOf('/password') !== -1 ||
            pathname.indexOf('/log') !== -1 ||
            pathname.indexOf('/pelanggaran') !== -1 ||
            pathname.indexOf('/account') !== -1 ||
            pathname.indexOf('/home') !== -1 ||
            pathname.indexOf('/selesai') !== -1 ||
            pathname.indexOf('/finish') !== -1;

        if (isNormal) {
            reportMode('normal');
            return;
        }

        var isExam = pathname.indexOf('/ujian') !== -1 ||
            pathname.indexOf('/exam') !== -1 ||
            pathname.indexOf('/soal') !== -1 ||
            pathname.indexOf('/cbt') !== -1 ||
            pathname.indexOf('/quiz') !== -1 ||
            pathname.indexOf('/mulai') !== -1 ||
            pathname.indexOf('/kerjakan') !== -1 ||
            pathname.indexOf('/assessment') !== -1 ||
            document.querySelector('.timer') !== null ||
            document.querySelector('.exam-timer') !== null;

        var isEssay = pathname.indexOf('/essay') !== -1 ||
            pathname.indexOf('/esai') !== -1 ||
            pathname.indexOf('/uraian') !== -1 ||
            document.getElementById('answerText') !== null ||
            document.querySelector('.cke_editor_answerText') !== null;

        if (isExam || isEssay) {
            reportMode('exam');
        } else {
            reportMode('normal');
        }
    }

    var hasReportedDomReady = false;

    function notifyDomReady() {
        try {
            var href = window.location.href || '';
            var ready = document.readyState === 'interactive' || document.readyState === 'complete';
            if (!hasReportedDomReady && href && href !== 'about:blank' && ready) {
                hasReportedDomReady = true;
                hostPost('onDomReady', [href]);
            }
        } catch (_) { }
    }

    document.addEventListener('click', function (e) {
        try {
            var target = e.target;
            while (target && target !== document && (!target.tagName || target.tagName.toUpperCase() !== 'A')) {
                target = target.parentNode;
            }
            if (!target || !target.href) {
                return;
            }

            var href = String(target.href).trim();
            if (!href) {
                return;
            }

            var lower = href.toLowerCase();
            var isOfficeScheme = lower.indexOf('ms-word:') === 0 ||
                lower.indexOf('ms-excel:') === 0 ||
                lower.indexOf('ms-powerpoint:') === 0 ||
                lower.indexOf('ms-office:') === 0;
            var cleanPath = lower.split('?')[0].split('#')[0];
            var isOfficeExt = cleanPath.endsWith('.doc') ||
                cleanPath.endsWith('.docx') ||
                cleanPath.endsWith('.xls') ||
                cleanPath.endsWith('.xlsx') ||
                cleanPath.endsWith('.ppt') ||
                cleanPath.endsWith('.pptx');

            if (isOfficeScheme || isOfficeExt) {
                e.preventDefault();
                e.stopPropagation();
                hostPost('onExternalOrOfficeUrl', [href]);
            }
        } catch (_) { }
    }, true);

    document.addEventListener('DOMContentLoaded', function () {
        notifyDomReady();
        detectStateFromDOM();
    });

    window.addEventListener('load', function () {
        notifyDomReady();
        detectStateFromDOM();
    });

    if (document.readyState === 'interactive' || document.readyState === 'complete') {
        setTimeout(function () {
            notifyDomReady();
            detectStateFromDOM();
        }, 50);
    }

    setInterval(function () {
        notifyDomReady();
        detectStateFromDOM();
    }, 2000);
})();
