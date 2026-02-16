// Profile dropdown close on outside click
document.addEventListener('click', function (e) {
    const dropdown = document.querySelector('[data-profile-dropdown]');
    if (dropdown) {
        // If click is outside dropdown, close it by clicking the button if menu is open
        if (!dropdown.contains(e.target)) {
            const button = dropdown.querySelector('button');
            const menu = dropdown.querySelector('.absolute');
            // Only click button if menu is visible
            if (menu && button) {
                button.click();
            }
        }
    }
});
