// Italian Date Input Mask (dd/MM/yyyy)
window.applyDateMask = (element) => {
    if (!element) return;
    
    element.addEventListener('input', function(e) {
        let value = e.target.value.replace(/\D/g, ''); // Remove non-digits
        let formatted = '';
        
        if (value.length > 0) {
            // Day
            formatted = value.substring(0, 2);
            
            if (value.length >= 3) {
                // Month
                formatted += '/' + value.substring(2, 4);
            }
            
            if (value.length >= 5) {
                // Year
                formatted += '/' + value.substring(4, 8);
            }
        }
        
        e.target.value = formatted;
    });
    
    element.addEventListener('keydown', function(e) {
        // Allow: backspace, delete, tab, escape, enter
        if ([8, 9, 27, 13, 46].indexOf(e.keyCode) !== -1 ||
            // Allow: Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X
            (e.keyCode === 65 && e.ctrlKey === true) ||
            (e.keyCode === 67 && e.ctrlKey === true) ||
            (e.keyCode === 86 && e.ctrlKey === true) ||
            (e.keyCode === 88 && e.ctrlKey === true) ||
            // Allow: home, end, left, right
            (e.keyCode >= 35 && e.keyCode <= 39)) {
            return;
        }
        
        // Ensure that it is a number and stop the keypress
        if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && 
            (e.keyCode < 96 || e.keyCode > 105)) {
            e.preventDefault();
        }
        
        // Limit to 10 characters (dd/MM/yyyy)
        if (e.target.value.replace(/\D/g, '').length >= 8 && 
            ![8, 9, 27, 13, 46, 37, 38, 39, 40].includes(e.keyCode)) {
            e.preventDefault();
        }
    });
};

// Italian DateTime Input Mask (dd/MM/yyyy HH:mm)
window.applyDateTimeMask = (element) => {
    if (!element) return;
    
    element.addEventListener('input', function(e) {
        let value = e.target.value.replace(/\D/g, ''); // Remove non-digits
        let formatted = '';
        
        if (value.length > 0) {
            // Day
            formatted = value.substring(0, 2);
            
            if (value.length >= 3) {
                // Month
                formatted += '/' + value.substring(2, 4);
            }
            
            if (value.length >= 5) {
                // Year
                formatted += '/' + value.substring(4, 8);
            }
            
            if (value.length >= 9) {
                // Hour
                formatted += ' ' + value.substring(8, 10);
            }
            
            if (value.length >= 11) {
                // Minute
                formatted += ':' + value.substring(10, 12);
            }
        }
        
        e.target.value = formatted;
    });
    
    element.addEventListener('keydown', function(e) {
        // Allow: backspace, delete, tab, escape, enter
        if ([8, 9, 27, 13, 46].indexOf(e.keyCode) !== -1 ||
            // Allow: Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X
            (e.keyCode === 65 && e.ctrlKey === true) ||
            (e.keyCode === 67 && e.ctrlKey === true) ||
            (e.keyCode === 86 && e.ctrlKey === true) ||
            (e.keyCode === 88 && e.ctrlKey === true) ||
            // Allow: home, end, left, right
            (e.keyCode >= 35 && e.keyCode <= 39)) {
            return;
        }
        
        // Ensure that it is a number and stop the keypress
        if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && 
            (e.keyCode < 96 || e.keyCode > 105)) {
            e.preventDefault();
        }
        
        // Limit to 12 digits (ddMMyyyyHHmm)
        if (e.target.value.replace(/\D/g, '').length >= 12 && 
            ![8, 9, 27, 13, 46, 37, 38, 39, 40].includes(e.keyCode)) {
            e.preventDefault();
        }
    });
};

// Apply mask to all date inputs with class 'date-mask-it'
window.initDateMasks = () => {
    document.querySelectorAll('.date-mask-it').forEach(input => {
        window.applyDateMask(input);
    });
    
    document.querySelectorAll('.datetime-mask-it').forEach(input => {
        window.applyDateTimeMask(input);
    });
};

// Helper to convert Italian date format to ISO (dd/MM/yyyy -> yyyy-MM-dd)
window.italianToIsoDate = (italianDate) => {
    if (!italianDate || italianDate.length !== 10) return null;
    
    const parts = italianDate.split('/');
    if (parts.length !== 3) return null;
    
    const day = parts[0];
    const month = parts[1];
    const year = parts[3];
    
    return `${year}-${month}-${day}`;
};

// Helper to convert ISO date to Italian format (yyyy-MM-dd -> dd/MM/yyyy)
window.isoToItalianDate = (isoDate) => {
    if (!isoDate || isoDate.length !== 10) return null;
    
    const parts = isoDate.split('-');
    if (parts.length !== 3) return null;
    
    const year = parts[0];
    const month = parts[1];
    const day = parts[2];
    
    return `${day}/${month}/${year}`;
};
